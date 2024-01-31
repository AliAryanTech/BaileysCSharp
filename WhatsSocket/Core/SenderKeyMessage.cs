﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using WhatsSocket.Core.Curve;

namespace WhatsSocket.Core
{
    public class CipherTextMessage
    {
        public const int UNSUPPORTED_VERSION = 1;
        public const int CURRENT_VERSION = 3;
        public const int WHISPER_TYPE = 2;
        public const int PREKEY_TYPE = 3;
        public const int SENDERKEY_TYPE = 4;
        public const int SENDERKEY_DISTRIBUTION_TYPE = 5;
        public const int ENCRYPTED_MESSAGE_OVERHEAD = 53;
    }

    internal class SenderKeyMessage : CipherTextMessage
    {
        public const int SIGNATURE_LENGTH = 64;
        public SenderKeyMessage(uint keyId, uint iteration, byte[] ciphertext, byte[] signatureKey)
        {
            KeyId = keyId;
            Iteration = iteration;
            Ciphertext = ciphertext;
            SignatureKey = signatureKey;
        }

        public SenderKeyMessage(byte[] serialized)
        {
            var version = serialized[0];
            byte[] message = serialized.Skip(1).Take(serialized.Length - SIGNATURE_LENGTH - 1).ToArray();
            byte[] signature = serialized.Skip(1 + message.Length).ToArray();


            var senderKeyMessage = Proto.SenderKeyMessage.Parser.ParseFrom(message);
            MessageVersion = (version & 0xff) >> 4;
            KeyId = senderKeyMessage.Id;
            Iteration = senderKeyMessage.Iteration;
            Ciphertext = senderKeyMessage.Ciphertext.ToByteArray();
            SignatureKey = signature;
            _serialized = serialized;
        }

        private byte[] _serialized;

        public int MessageVersion { get; set; }
        public uint KeyId { get; }
        public uint Iteration { get; }
        public byte[] Ciphertext { get; }
        public byte[] SignatureKey { get; }

        internal void VerifySignature(byte[] signatureKey)
        {
            byte[] part1 = _serialized.Take(_serialized.Length - SIGNATURE_LENGTH ).ToArray();
            byte[] part2 = _serialized.Skip(part1.Length).ToArray();
            var valid = Curve25519.Verify(signatureKey.Skip(1).ToArray(), part1, part2);
            if (!valid)
            {
                throw new GroupCipherException("Invalid Signature");
            }
        }
    }
}
