using System.Security.Cryptography;
using System.Text;

namespace TongDaiNoiBo.Security
{
    public class RsaService
    {
        // Tạo cặp khóa RSA
        public (string PublicKey, string PrivateKey) TaoCapKhoaRSA()
        {
            using (RSA rsa = RSA.Create(2048))
            {
                string publicKey = Convert.ToBase64String(
                    rsa.ExportRSAPublicKey()
                );

                string privateKey = Convert.ToBase64String(
                    rsa.ExportRSAPrivateKey()
                );

                return (publicKey, privateKey);
            }
        }

        // Tạo chữ ký số bằng Private Key
        public string KyDuLieu(
            string duLieu,
            string privateKeyBase64)
        {
            byte[] privateKey =
                Convert.FromBase64String(privateKeyBase64);

            using (RSA rsa = RSA.Create())
            {
                rsa.ImportRSAPrivateKey(
                    privateKey,
                    out _
                );

                byte[] duLieuBytes =
                    Encoding.UTF8.GetBytes(duLieu);

                byte[] chuKy = rsa.SignData(
                    duLieuBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );

                return Convert.ToBase64String(chuKy);
            }
        }

        // Kiểm tra chữ ký bằng Public Key
        public bool KiemTraChuKy(
            string duLieu,
            string chuKyBase64,
            string publicKeyBase64)
        {
            try
            {
                byte[] publicKey =
                    Convert.FromBase64String(publicKeyBase64);

                byte[] chuKy =
                    Convert.FromBase64String(chuKyBase64);

                byte[] duLieuBytes =
                    Encoding.UTF8.GetBytes(duLieu);

                using (RSA rsa = RSA.Create())
                {
                    rsa.ImportRSAPublicKey(
                        publicKey,
                        out _
                    );

                    return rsa.VerifyData(
                        duLieuBytes,
                        chuKy,
                        HashAlgorithmName.SHA256,
                        RSASignaturePadding.Pkcs1
                    );
                }
            }
            catch
            {
                return false;
            }
        }
    }
}