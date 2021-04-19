using System;
using System.IO;
using Org.BouncyCastle.Bcpg.OpenPgp;
using Org.BouncyCastle.Utilities.IO;

namespace CMI.Manager.Onboarding
{
    /// <summary>
    ///     Klasse zum entschlüsslen von Datein, die mit PGP verschlüsselt sind.
    /// 
    ///     Quellcode basierend auf ChoPGP: https://github.com/Cinchoo/ChoPGP
    ///     (ChoPGP is licensed under the MIT License.)
    /// </summary>
    public class Pgp
    {
        /// <summary>
        ///     PGP decrypt a given stream
        /// </summary>
        public static void Decrypt(Stream inputStream, Stream outputStream, Stream privateKeyStream, string passPhrase)
        {
            if (inputStream == null)
            {
                throw new ArgumentException("InputStream");
            }

            if (outputStream == null)
            {
                throw new ArgumentException("outputStream");
            }

            if (privateKeyStream == null)
            {
                throw new ArgumentException("privateKeyStream");
            }

            if (passPhrase == null)
            {
                passPhrase = string.Empty;
            }

            var objFactory = new PgpObjectFactory(PgpUtilities.GetDecoderStream(inputStream));
            // find secret key
            var pgpSec = new PgpSecretKeyRingBundle(PgpUtilities.GetDecoderStream(privateKeyStream));

            PgpObject obj = null;
            if (objFactory != null)
            {
                obj = objFactory.NextPgpObject();
            }

            // the first object might be a PGP marker packet.
            PgpEncryptedDataList enc = null;
            if (obj is PgpEncryptedDataList)
            {
                enc = (PgpEncryptedDataList) obj;
            }
            else
            {
                enc = (PgpEncryptedDataList) objFactory.NextPgpObject();
            }

            // decrypt
            PgpPrivateKey privateKey = null;
            PgpPublicKeyEncryptedData pbe = null;
            foreach (PgpPublicKeyEncryptedData pked in enc.GetEncryptedDataObjects())
            {
                privateKey = FindPgpSecretKey(pgpSec, pked.KeyId, passPhrase.ToCharArray());

                if (privateKey != null)
                {
                    pbe = pked;
                    break;
                }
            }

            if (privateKey == null)
            {
                throw new ArgumentException("Secret key for message not found.");
            }

            PgpObjectFactory plainFact = null;

            using (var clear = pbe.GetDataStream(privateKey))
            {
                plainFact = new PgpObjectFactory(clear);
            }

            var message = plainFact.NextPgpObject();
            if (message is PgpOnePassSignatureList)
            {
                message = plainFact.NextPgpObject();
            }

            if (message is PgpCompressedData)
            {
                var cData = (PgpCompressedData) message;
                PgpObjectFactory of = null;

                using (var compDataIn = cData.GetDataStream())
                {
                    of = new PgpObjectFactory(compDataIn);
                }

                message = of.NextPgpObject();
                if (message is PgpOnePassSignatureList)
                {
                    message = of.NextPgpObject();
                    PgpLiteralData Ld = null;
                    Ld = (PgpLiteralData) message;
                    var unc = Ld.GetInputStream();
                    Streams.PipeAll(unc, outputStream);
                }
                else
                {
                    PgpLiteralData Ld = null;
                    Ld = (PgpLiteralData) message;
                    var unc = Ld.GetInputStream();
                    Streams.PipeAll(unc, outputStream);
                }
            }
            else if (message is PgpLiteralData)
            {
                var ld = (PgpLiteralData) message;
                var outFileName = ld.FileName;

                var unc = ld.GetInputStream();
                Streams.PipeAll(unc, outputStream);
            }
            else if (message is PgpOnePassSignatureList)
            {
                throw new PgpException("Encrypted message contains a signed message - not literal data.");
            }
            else
            {
                throw new PgpException("Message is not a simple encrypted file.");
            }
        }

        /// <summary>
        ///     Search a secret key ring collection for a secret key corresponding to keyId if it exists.
        /// </summary>
        private static PgpPrivateKey FindPgpSecretKey(PgpSecretKeyRingBundle pgpSec, long keyId, char[] pass)
        {
            var pgpSecKey = pgpSec.GetSecretKey(keyId);

            if (pgpSecKey == null)
            {
                return null;
            }

            return pgpSecKey.ExtractPrivateKey(pass);
        }
    }
}