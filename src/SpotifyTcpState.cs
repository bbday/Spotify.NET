using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using CPlayerLib;
using Google.Protobuf;
using Nito.AsyncEx;
using Org.BouncyCastle.Math;
using Org.BouncyCastle.Utilities;
using SpotifyNET.Cryptography;
using SpotifyNET.Enums;
using SpotifyNET.Exceptions;
using SpotifyNET.Helpers;
using SpotifyNET.Interfaces;
using SpotifyNET.Models;

namespace SpotifyNET;


internal class SpotifyTcpState : ISpotifyTcpState,
    IDisposable
{

    public SpotifyTcpState(string host,
        ushort port) 
    {
        ConnectedHostUrl = host;
        ConnectedHostPort = port;
        TcpClient = new TcpClient(host, port)
        {
            ReceiveTimeout = 500
        };
    }
    
    public TcpClient TcpClient { get; }
    private Shannon ReceiveCipher;
    private Shannon SendCipher;
    private AsyncLock ReceiveLock = new AsyncLock();
    private AsyncLock SendLock = new AsyncLock();
    
    
    public string ConnectedHostUrl { get; }
    public ushort ConnectedHostPort { get; }

    public bool IsConnected
    {
        get
        {
            try
            {
                return TcpClient is
                {
                    Connected: true
                } && TcpClient.GetStream().ReadTimeout > -2;
            }
            catch (Exception)
            {
                return false;
            }
        }
    }

    /// <summary>
    ///     Opens a TCP connection to spotify and connects but does not authenticate.
    /// </summary>
    /// <param name="ct">Cancellation token for the asynchronous task.</param>
    /// <returns></returns>
    /// <exception cref="IOException">
    ///     Thrown when an issue occurs with the underlying socket and may not be Spotify's issue.
    /// </exception>
    /// <exception cref="AccessViolationException">
    ///     Thrown when a handshake could not be verified. This can be due to a compromised network.
    /// </exception>
    /// <exception cref="SpotifyConnectionException">
    ///     Thrown when bad data is returned from Spotify.
    ///     This usually means something went wrong in the connection and a new one has to be established.
    /// </exception>
    public async Task ConnectToTcpClient(CancellationToken ct = default)
    {
        var keys = new DiffieHellman();
        var clientHello = GetClientHello(keys);

        var clientHelloBytes = clientHello.ToByteArray();
        var networkStream = TcpClient.GetStream();

        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x04);
        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x00);
        await networkStream.FlushAsync(ct);

        var length = 2 + 4 + clientHelloBytes.Length;
        var bytes = BitConverter.GetBytes(length);

        networkStream.WriteByte(bytes[0]);
        await networkStream.WriteAsync(clientHelloBytes, 0, clientHelloBytes.Length, ct);
        await networkStream.FlushAsync(ct);


        var buffer = new byte[1000];

        var len = int.Parse(networkStream.Read(buffer,
            0, buffer.Length).ToString());
        var tmp = new byte[len];
        Array.Copy(buffer, tmp, len);

        tmp = tmp.Skip(4).ToArray();
        using var accumulator = new MemoryStream();
        accumulator.WriteByte(0x00);
        accumulator.WriteByte(0x04);

        var lnarr = length.ToByteArray();
        await accumulator.WriteAsync(lnarr, 0, lnarr.Length, ct);
        await accumulator.WriteAsync(clientHelloBytes, 0, clientHelloBytes.Length, ct);

        var lenArr = len.ToByteArray();
        await accumulator.WriteAsync(lenArr, 0, lenArr.Length, ct);
        await accumulator.WriteAsync(tmp, 0, tmp.Length, ct);
        accumulator.Position = 0;

        var apResponseMessage = APResponseMessage.Parser.ParseFrom(tmp);
        var sharedKey = ByteExtensions.ToByteArray(keys.ComputeSharedKey(apResponseMessage
            .Challenge.LoginCryptoChallenge.DiffieHellman.Gs.ToByteArray()));
        // Check gs_signature
        var rsa = new RSACryptoServiceProvider();
        var rsaKeyInfo = new RSAParameters
        {
            Modulus = new BigInteger(Consts.ServerKey).ToByteArrayUnsigned(),
            Exponent = BigInteger.ValueOf(65537).ToByteArrayUnsigned()
        };

        //Import key parameters into RSA.
        rsa.ImportParameters(rsaKeyInfo);
        var gs = apResponseMessage.Challenge.LoginCryptoChallenge.DiffieHellman.Gs.ToByteArray();
        var sign = apResponseMessage.Challenge.LoginCryptoChallenge.DiffieHellman.GsSignature.ToByteArray();

        if (!rsa.VerifyData(gs,
                sign,
                HashAlgorithmName.SHA1, RSASignaturePadding.Pkcs1))
            throw new AccessViolationException("Failed to verify APResponse");

        // Solve challenge
        var binaryData = accumulator.ToArray();
        using var data = new MemoryStream();
        var mac = new HMACSHA1(sharedKey);
        mac.Initialize();
        for (var i = 1; i < 6; i++)
        {
            mac.TransformBlock(binaryData, 0, binaryData.Length, null, 0);
            var temp = new[] {(byte) i};
            mac.TransformBlock(temp, 0, temp.Length, null, 0);
            mac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
            var final = mac.Hash;
            data.Write(final, 0, final.Length);
            mac = new HMACSHA1(sharedKey);
        }

        var dataArray = data.ToArray();
        mac = new HMACSHA1(Arrays.CopyOfRange(dataArray, 0, 0x14));
        mac.TransformBlock(binaryData, 0, binaryData.Length, null, 0);
        mac.TransformFinalBlock(Array.Empty<byte>(), 0, 0);
        var challenge = mac.Hash;
        var clientResponsePlaintext = new ClientResponsePlaintext
        {
            LoginCryptoResponse = new LoginCryptoResponseUnion
            {
                DiffieHellman = new LoginCryptoDiffieHellmanResponse
                {
                    Hmac = ByteString.CopyFrom(challenge)
                }
            },
            PowResponse = new PoWResponseUnion(),
            CryptoResponse = new CryptoResponseUnion()
        };
        var clientResponsePlaintextBytes = clientResponsePlaintext.ToByteArray();
        var len2 = 4 + clientResponsePlaintextBytes.Length;

        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x00);
        networkStream.WriteByte(0x00);
        var bytesb = BitConverter.GetBytes(len2);
        networkStream.WriteByte(bytesb[0]);
        networkStream.Write(clientResponsePlaintextBytes, 0, clientResponsePlaintextBytes.Length);
        await networkStream.FlushAsync(ct);

        if (networkStream.DataAvailable)
            //if data is available, it could be scrap or a failed login.
            try
            {
                var scrap = new byte[4];
                networkStream.ReadTimeout = 300;
                var read = networkStream.Read(scrap, 0, scrap.Length);
                if (read == scrap.Length)
                {
                    var lengthOfScrap = (scrap[0] << 24) | (scrap[1] << 16) | (scrap[2] << 8) |
                                        (scrap[3] & 0xFF);
                    var payload = new byte[length - 4];
                    await networkStream.ReadCompleteAsync(payload, 0, payload.Length, ct);
                    var failed = APResponseMessage.Parser.ParseFrom(payload);
                    throw new SpotifyConnectionException(failed);
                }

                if (read > 0) throw new UnknownDataException(scrap);
            }
            catch (Exception x)
            {
                // ignored
            }

        SendCipher = new Shannon();
        SendCipher.key(Arrays.CopyOfRange(data.ToArray(), 0x14, 0x34));

        ReceiveCipher = new Shannon();
        ReceiveCipher.key(Arrays.CopyOfRange(data.ToArray(), 0x34, 0x54));

        networkStream.ReadTimeout = Timeout.Infinite;
    }

    
    /// <summary>
    /// Fire and forget package sending. Somewhat async but mostly relies on Task.Run()
    /// </summary>
    /// <param name="packet"></param>
    /// <returns></retuprns>
    public async ValueTask SendPackageAsync(
        MercuryPacket packet,
        CancellationToken ct = default)
    {
        var payload = packet.Payload;
        var cmd = packet.Cmd;
        using (await SendLock.LockAsync(ct))
        {
            var payloadLengthAsByte = BitConverter.GetBytes((short) payload.Length).Reverse().ToArray();
            using var yetAnotherBuffer = new MemoryStream(3 + payload.Length);
            yetAnotherBuffer.WriteByte((byte) cmd);
            await yetAnotherBuffer.WriteAsync(payloadLengthAsByte, 0, payloadLengthAsByte.Length, ct);
            await yetAnotherBuffer.WriteAsync(payload, 0, payload.Length, ct);

            SendCipher.nonce(SendCipher.Nonce.ToByteArray());
            Interlocked.Increment(ref SendCipher.Nonce);

            var bufferBytes = yetAnotherBuffer.ToArray();
            SendCipher.encrypt(bufferBytes);

            var fourBytesBuffer = new byte[4];
            SendCipher.finish(fourBytesBuffer);

            var networkStream = TcpClient.GetStream();
            networkStream.Write(bufferBytes, 0, bufferBytes.Length);
            networkStream.Write(fourBytesBuffer, 0, fourBytesBuffer.Length);
            await networkStream.FlushAsync(ct);
        }
    }
    
    
    /// <summary>
    /// Waits and receives a package (blocking function)
    /// </summary>
    /// <returns></returns>
    public  async ValueTask<MercuryPacket> ReceivePackageAsync(
        CancellationToken ct)
    {
        using (await ReceiveLock.LockAsync(ct))
        {
            ReceiveCipher.nonce(ReceiveCipher.Nonce.ToByteArray());
            Interlocked.Increment(ref ReceiveCipher.Nonce);

            var headerBytes = new byte[3];
            var networkStream = TcpClient.GetStream();

            await networkStream.ReadCompleteAsync(headerBytes, 0,
                headerBytes.Length, ct);
            ReceiveCipher.decrypt(headerBytes);

            var cmd = headerBytes[0];
            var payloadLength = (short) ((headerBytes[1] << 8) | (headerBytes[2] & 0xFF));

            var payloadBytes = new byte[payloadLength];
            await networkStream.ReadCompleteAsync(payloadBytes, 0, payloadBytes.Length, ct);
            ReceiveCipher.decrypt(payloadBytes);

            var mac = new byte[4];
            await networkStream.ReadCompleteAsync(mac, 0, mac.Length, ct: ct);

            var expectedMac = new byte[4];
            ReceiveCipher.finish(expectedMac);
            return new MercuryPacket((MercuryPacketType) cmd, payloadBytes);
        }
    }
    
    public void Dispose()
    {
        TcpClient?.Dispose();
    }
    
    private static ClientHello GetClientHello(DiffieHellman publickey)
    {
        var clientHello = new ClientHello
        {
            BuildInfo = new BuildInfo
            {
                Platform = Platform.Win32X86,
                Product = Product.Client,
                ProductFlags = {ProductFlags.ProductFlagNone},
                Version = 112800721
            }
        };


        clientHello.CryptosuitesSupported.Add(Cryptosuite.Shannon);
        clientHello.LoginCryptoHello = new LoginCryptoHelloUnion
        {
            DiffieHellman = new LoginCryptoDiffieHellmanHello
            {
                Gc = ByteString.CopyFrom(publickey.PublicKeyArray()),
                ServerKeysKnown = 1
            }
        };
        var nonce = new byte[16];
        new Random().NextBytes(nonce);
        clientHello.ClientNonce = ByteString.CopyFrom(nonce);
        clientHello.Padding = ByteString.CopyFrom(30);

        return clientHello;
    }

}