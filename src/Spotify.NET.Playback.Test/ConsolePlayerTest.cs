using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Connectstate;
using CPlayerLib.Player.Proto;
using CPlayerLib.Player.Proto.Transfer;
using SpotifyNET;
using SpotifyNET.Enums;
using SpotifyNET.Interfaces;
using SpotifyNET.Models;
using static Spotify.NET.Playback.Test.LogHelper;
using ContextPlayerOptions = Connectstate.ContextPlayerOptions;
using PlayOrigin = Connectstate.PlayOrigin;

namespace Spotify.NET.Playback.Test
{
    public class ConsolePlayerTest : ISpotifyPlayer
    {
        private readonly SpotifyClient _client;
        public ConsolePlayerTest(SpotifyClient client)
        {
            _client = client;
        }

        public PlayerState State { get; set; }
        public PutStateRequest PutState { get; set; }

        public SpotifyPlayback CurrentPlayback { get; private set; }

        public async Task<RequestResult> IncomingCommand(Endpoint endpoint, CommandBody? data)
        {
            switch (endpoint)
            {
                case Endpoint.Transfer:
                    var transferState = TransferState.Parser.ParseFrom(data!.Value.Data);
                    var context = new SpotifyId(transferState.CurrentSession.Context.Uri);
                    Log($"Loading context (transfer), uri {context.Uri}");


                    //First we convert things like the play origin.
                    var playOrigin = transferState.CurrentSession.PlayOrigin;
                    State.PlayOrigin = new PlayOrigin
                    {
                        DeviceIdentifier = playOrigin.DeviceIdentifier,
                        ExternalReferrer = playOrigin.ExternalReferrer,
                        FeatureIdentifier = playOrigin.FeatureIdentifier,
                        FeatureVersion = playOrigin.FeatureVersion,
                        ReferrerIdentifier = playOrigin.ReferrerIdentifier
                    };
                    State.PlayOrigin.FeatureClasses.AddRange(playOrigin.FeatureClasses);

                    //Set options like repeating track, context, shuffling etc.
                    var options = transferState.Options;
                    State.Options = new ContextPlayerOptions
                    {
                        RepeatingContext = options.RepeatingContext,
                        RepeatingTrack = options.RepeatingTrack,
                        ShufflingContext = options.ShufflingContext
                    };

                    var pb = transferState.Playback;
                    SpotifyId trackId = default;
                    if (pb.CurrentTrack.HasUri && !string.IsNullOrEmpty(pb.CurrentTrack.Uri))
                    {
                        trackId = new SpotifyId(pb.CurrentTrack.Uri);
                    }
                    else if (pb.CurrentTrack.HasGid)
                    {
                        trackId = SpotifyId.FromGid(pb.CurrentTrack.Gid, context.Type switch
                        {
                            AudioItemType.Show => AudioItemType.Episode,
                            AudioItemType.Episode => AudioItemType.Episode,
                            _ => AudioItemType.Track
                        });
                    }

                    var ctx = new SpotifyContext(transferState.CurrentSession.Context.Uri);
                    State.ContextUri = transferState.CurrentSession.Context.Uri;
                    if (!ctx.IsFinite)
                    {
                        SetRepeatingContext(ctx, false);
                        SetShufflingContext(ctx, false);
                    }

                    State.ContextUrl = string.Empty;
                    State.Restrictions = new global::Connectstate.Restrictions();
                    State.ContextMetadata.Clear();

                    SetIsActive(true);

                    var sessionId = GenerateSessionId();


                    State.PositionAsOfTimestamp = pb.PositionAsOfTimestamp;
                    State.Timestamp = pb.IsPaused ? TimeHelper.CurrentTimeMillisSystem : pb.Timestamp;

                    CurrentPlayback = new SpotifyPlayback
                    {
                        Context = ctx,
                        Id = trackId,
                        IsPaused = pb.IsPaused,
                        Repeating = transferState.Options.RepeatingTrack
                            ? Repeating.Track
                            : transferState.Options.RepeatingContext
                                ? Repeating.Context
                                : Repeating.None,
                        SessionId = sessionId,
                        Shuffle = transferState.Options.ShufflingContext
                    };
                    //var playback = await 
                    //    _client.StreamItemAsync(trackId, AudioQualityExtensions.AudioQuality.HIGH);
                    return RequestResult.Success;
                default:
                    return RequestResult.DeviceDoesNotSupportCommand;
            }
        }

        private void SetShufflingContext(SpotifyContext context, bool value)
        {
            var old = State.Options.ShufflingContext;
            State.Options.ShufflingContext = value && context.Restrictions.Can(RestrictionsManager.Action.SHUFFLE);

            //TODO: Shuffle actual tracks.
            //if (old != State.Options.ShufflingContext) 
            // tracksKeeper.toggleShuffle(isShufflingContext());
        }
        private void SetRepeatingContext(SpotifyContext context,
            bool value)
        {
            State.Options.RepeatingContext =
                value && context.Restrictions.Can(RestrictionsManager.Action.REPEAT_CONTEXT);
        }
        private void SetRepeatingTrack(SpotifyContext context,
            bool value)
        {
            State.Options.RepeatingTrack =
                value && context.Restrictions.Can(RestrictionsManager.Action.REPEAT_TRACK);
        }
        private void SetIsActive(bool active)
        {
            if (active)
            {
                if (!PutState.IsActive)
                {
                    var now = TimeHelper.CurrentTimeMillisSystem;
                    PutState.IsActive = true;
                    PutState.StartedPlayingAt = (ulong)now;
                    Log($"Device is now active. ts: {now}");
                }
            }
            else
            {
                PutState.IsActive = false;
                PutState.StartedPlayingAt = 0L;
            }
        }
        private static string GenerateSessionId()
        {
            var bytes = new byte[16];
            (new Random()).NextBytes(bytes);
            var str = Base64UrlEncode(bytes);
            return str;
        }
        private static string Base64UrlEncode(byte[] inputBytes)
        {
            // Special "url-safe" base64 encode.
            return Convert.ToBase64String(inputBytes)
                .Replace('+', '-') // replace URL unsafe characters with safe ones
                .Replace('/', '_') // replace URL unsafe characters with safe ones
                .Replace("=", ""); // no padding
        }
    }

    public struct SpotifyPlayback
    {
        public SpotifyId Id { get; init; }
        public SpotifyContext Context { get; init; }
        public SpotifyQueueTrack[] Queue { get; init; }

        public bool IsPaused { get; init; }
        public Repeating Repeating { get; init; }
        public bool Shuffle { get; init; }

        public string SessionId { get; init; }

    }

    public struct SpotifyQueueTrack
    {
        public SpotifyId Id { get; init; }
    }

    public struct SpotifyContext
    {
        public SpotifyContext(string context) : this()
        {
            Context = context;
            IsFinite = !context.StartsWith("spotify:dailymix:") || !context.StartsWith("spotify:station:");
            Restrictions = new RestrictionsManager(this);
        }
        public string Context { get; }
        public RestrictionsManager Restrictions { get; }
        public bool IsFinite { get; }
    }
    public enum Repeating
    {
        None,
        Context,
        Track
    }

    public static class TracksHelper
    {
        public static SpotifyQueueTrack[] InitializeFrom(Func<List<ContextTrack>, int> finder, ContextTrack track,
            Queue contextQueue)
        {
            while (true)
            {

            }
        }

        public static async Task<bool> NextPage()
        {
            return false;
        }
    }
}
