﻿using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CPlayerLib;
using Google.Protobuf;
using Nito.AsyncEx;
using SpotifyNET.Enums;
using SpotifyNET.Exceptions;
using SpotifyNET.Helpers;
using SpotifyNET.Interfaces;
using SpotifyNET.Models;
using SpotifyNET.OneTimeStructures;

namespace System.Runtime.CompilerServices
{
    internal static class IsExternalInit
    {
    }
}

namespace SpotifyNET
{
    public class SpotifyClient : ISpotifyClient, IDisposable
    {
        private readonly IAuthenticator _authenticator;
        internal AsyncLock TokenLock = new AsyncLock();
        internal ConcurrentBag<MercuryToken> Tokens = new ConcurrentBag<MercuryToken>();

        /// <summary>
        /// Create a new instance of SpotifyClient. 
        /// </summary>
        /// <param name="authenticator">
        ///     Use TODO: UserpassAuthenticator or StoredAuthAuthenticator.
        /// </param>
        /// <param name="config"></param>
        public SpotifyClient(IAuthenticator authenticator,
            SpotifyConfig config)
        {
            _authenticator = authenticator;
            Config = config;
        }

        public async Task<APWelcome> ConnectAndAuthenticateAsync(CancellationToken ct = default)
        {
            (string host, ushort port) accessPoint;
            try
            {
                var accessPoints
                    = await ApResolver.GetClosestAccessPoint(ct);
                if (accessPoints is not
                    {
                        Length : > 0
                    })
                    throw new NoAccessPointFoundException(NoAccessPointFoundReasonType.ReturnedZero, null);

                accessPoint = accessPoints.First();
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.ToString());
                throw new NoAccessPointFoundException(NoAccessPointFoundReasonType.Exception, ex);
            }

            Debug.WriteLine($"Fetched: {accessPoint.host}:{accessPoint.port} as AP.");


            var tcpState = new SpotifyTcpState(accessPoint.host, accessPoint.port);
            TcpState = tcpState;
            await TcpState.ConnectToTcpClient(ct);

            var credentials = await
                _authenticator.GetAsync(ct);

            var clientResponseEncrypted =
                new ClientResponseEncrypted
                {
                    LoginCredentials = credentials,
                    SystemInfo = new SystemInfo
                    {
                        Os = Os.Windows,
                        CpuFamily = CpuFamily.CpuX86,
                        SystemInformationString = "1",
                        DeviceId = Config.DeviceId
                    },
                    VersionString = "1.0"
                };

            await TcpState.SendPackageAsync(new MercuryPacket(MercuryPacketType.Login,
                clientResponseEncrypted.ToByteArray()), ct);


            var packet = await
                TcpState.ReceivePackageAsync(ct);

            switch (packet.Cmd)
            {
                case MercuryPacketType.APWelcome:
                    await UpdateLocaleAsync(Config.Locale, ct);
                    tcpState.StartListeningForPackages();
                    ApWelcome = APWelcome.Parser.ParseFrom(packet.Payload);
                    return ApWelcome;
                case MercuryPacketType.AuthFailure:
                    throw new SpotifyAuthenticationException(APLoginFailed.Parser.ParseFrom(packet.Payload));
                default:
                    throw new UnknownDataException($"Invalid package type: {packet.Cmd}", packet.Payload);
            }
        }

        public async ValueTask<MercuryToken> GetBearerAsync(CancellationToken ct = default)
        {
            using (await TokenLock.LockAsync(ct))
            {
                var tokenOrDefault =
                    FindNonExpiredToken();
                if (tokenOrDefault.HasValue) return tokenOrDefault.Value;

                var newToken = await SendAndReceiveAsJson<MercuryToken>(
                    "hm://keymaster/token/authenticated?scope=playlist-read" +
                    $"&client_id={Consts.KEYMASTER_CLIENT_ID}&device_id=", ct: ct);
                Tokens.Add(newToken);
                return newToken;
            }
        }

        public async ValueTask<T> SendAndReceiveAsJson<T>(
            string mercuryUri,
            MercuryRequestType type = MercuryRequestType.Get,
            CancellationToken ct = default)
        {
            var response = await TcpState.SendAndReceiveAsResponse(mercuryUri, type, ct);
            if (response is {StatusCode: >= 200 and < 300})
            {
                return Deserialize<T>(response.Value);
            }

            throw new MercuryException(response);
        }
        
        
        
        public async ValueTask UpdateLocaleAsync(string locale, CancellationToken ct = default)
        {
            if (TcpState is not
                {
                    IsConnected: true
                }) throw new InvalidOperationException("Not connected to spotify.");
            using var preferredLocale = new MemoryStream(18 + 5);
            preferredLocale.WriteByte(0x0);
            preferredLocale.WriteByte(0x0);
            preferredLocale.WriteByte(0x10);
            preferredLocale.WriteByte(0x0);
            preferredLocale.WriteByte(0x02);
            preferredLocale.Write("preferred-locale");
            preferredLocale.Write(locale);
            await TcpState.SendPackageAsync(new MercuryPacket(MercuryPacketType.PreferredLocale,
                preferredLocale.ToArray()), ct);
        }


        /// <summary>
        /// Contains metadata about the current authenticated user, such as their spotify id, and reusable auth credentials.
        /// </summary>
        public APWelcome? ApWelcome { get; private set; }

        /// <summary>
        /// The active TCP connection to the spotify servers.
        /// </summary>
        public ISpotifyTcpState? TcpState { get; private set; }

        public bool IsConnected => ApWelcome != null && (TcpState?.IsConnected ?? false);
        public SpotifyConfig Config { get; private set; }

        public void Dispose()
        {
            TcpState?.Dispose();
        }    
        internal MercuryToken? FindNonExpiredToken()
        {
            var a =
                Tokens.FirstOrDefault(token => !token.IsExpired());
            if (string.IsNullOrEmpty(a.AccessToken)) return null;
            return a;
        }
        
        private static T Deserialize<T>(MercuryResponse resp) =>
            System.Text.Json.JsonSerializer.Deserialize<T>(
                new ReadOnlySpan<byte>(resp.Payload.SelectMany(z => z).ToArray()), opts);
        
        public static readonly JsonSerializerOptions opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true
        };
    }
}