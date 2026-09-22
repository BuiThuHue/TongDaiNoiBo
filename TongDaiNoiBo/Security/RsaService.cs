using System.Security.Cryptography;
using System.Text;

namespace TongDaiNoiBo.Security
{
    public class RsaService
    {
        // =========================================================
        // TẠO CẶP KHÓA RSA
        //
        // Hàm này vẫn giữ để tương thích với phần code cũ.
        // Sau này việc tạo khóa chính sẽ thực hiện ở trình duyệt.
        // =========================================================

        public (string PublicKey, string PrivateKey) TaoCapKhoaRSA()
        {
            using RSA rsa = RSA.Create(2048);

            string publicKey =
                Convert.ToBase64String(
                    rsa.ExportRSAPublicKey()
                );

            string privateKey =
                Convert.ToBase64String(
                    rsa.ExportRSAPrivateKey()
                );

            return (
                publicKey,
                privateKey
            );
        }


        // =========================================================
        // KÝ DỮ LIỆU
        //
        // Giữ lại để tương thích với phần B hiện tại.
        //
        // Sau khi hoàn thiện:
        // Private Key sẽ ký ở trình duyệt,
        // Server không cần dùng hàm này cho cuộc gọi nữa.
        // =========================================================

        public string KyDuLieu(
            string duLieu,
            string privateKeyBase64)
        {
            byte[] privateKey =
                Convert.FromBase64String(
                    privateKeyBase64
                );

            using RSA rsa =
                RSA.Create();


            // -----------------------------------------------------
            // THỬ PKCS#1 TRƯỚC
            // -----------------------------------------------------

            try
            {
                rsa.ImportRSAPrivateKey(
                    privateKey,
                    out _
                );
            }
            catch (CryptographicException)
            {
                // -------------------------------------------------
                // Nếu Private Key đến từ Web Crypto
                // thì thường là PKCS#8.
                // -------------------------------------------------

                rsa.ImportPkcs8PrivateKey(
                    privateKey,
                    out _
                );
            }


            byte[] duLieuBytes =
                Encoding.UTF8.GetBytes(
                    duLieu
                );


            byte[] chuKy =
                rsa.SignData(
                    duLieuBytes,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );


            return Convert.ToBase64String(
                chuKy
            );
        }


        // =========================================================
        // KIỂM TRA CHỮ KÝ RSA
        //
        // Hỗ trợ:
        //
        // 1. PKCS#1
        //    → Public Key cũ do C# tạo
        //
        // 2. SPKI
        //    → Public Key mới do trình duyệt Web Crypto tạo
        // =========================================================

        public bool KiemTraChuKy(
            string duLieu,
            string chuKyBase64,
            string publicKeyBase64)
        {
            try
            {
                byte[] publicKey =
                    Convert.FromBase64String(
                        publicKeyBase64
                    );


                byte[] chuKy =
                    Convert.FromBase64String(
                        chuKyBase64
                    );


                byte[] duLieuBytes =
                    Encoding.UTF8.GetBytes(
                        duLieu
                    );


                using RSA rsa =
                    RSA.Create();


                bool daImport =
                    false;


                // =================================================
                // THỬ PUBLIC KEY PKCS#1
                // =================================================

                try
                {
                    rsa.ImportRSAPublicKey(
                        publicKey,
                        out _
                    );

                    daImport =
                        true;
                }
                catch (CryptographicException)
                {
                    // Không phải PKCS#1.
                }


                // =================================================
                // NẾU KHÔNG PHẢI PKCS#1
                // → THỬ SPKI
                //
                // Web Crypto exportKey("spki")
                // sẽ đi vào đây.
                // =================================================

                if (!daImport)
                {
                    rsa.ImportSubjectPublicKeyInfo(
                        publicKey,
                        out _
                    );
                }


                // =================================================
                // VERIFY
                // =================================================

                return rsa.VerifyData(
                    duLieuBytes,
                    chuKy,
                    HashAlgorithmName.SHA256,
                    RSASignaturePadding.Pkcs1
                );
            }
            catch
            {
                return false;
            }
        }


        // =========================================================
        // MÃ HÓA CHỮ KÝ BẰNG AES-GCM
        // =========================================================

        public string MaHoaChuKy(
            string chuKy,
            string khoaBiMat)
        {
            byte[] key =
                TaoKhoaAES(
                    khoaBiMat
                );


            byte[] iv =
                RandomNumberGenerator
                    .GetBytes(12);


            byte[] duLieu =
                Convert.FromBase64String(
                    chuKy
                );


            byte[] cipherText =
                new byte[
                    duLieu.Length
                ];


            byte[] tag =
                new byte[16];


            using AesGcm aes =
                new AesGcm(
                    key,
                    16
                );


            aes.Encrypt(
                iv,
                duLieu,
                cipherText,
                tag
            );


            byte[] ketQua =
                new byte[
                    iv.Length +
                    tag.Length +
                    cipherText.Length
                ];


            Buffer.BlockCopy(
                iv,
                0,
                ketQua,
                0,
                iv.Length
            );


            Buffer.BlockCopy(
                tag,
                0,
                ketQua,
                iv.Length,
                tag.Length
            );


            Buffer.BlockCopy(
                cipherText,
                0,
                ketQua,
                iv.Length + tag.Length,
                cipherText.Length
            );


            return Convert.ToBase64String(
                ketQua
            );
        }


        // =========================================================
        // GIẢI MÃ CHỮ KÝ AES-GCM
        // =========================================================

        public string GiaiMaChuKy(
            string chuKyDaMaHoa,
            string khoaBiMat)
        {
            byte[] key =
                TaoKhoaAES(
                    khoaBiMat
                );


            byte[] duLieu =
                Convert.FromBase64String(
                    chuKyDaMaHoa
                );


            if (duLieu.Length < 28)
            {
                throw new CryptographicException(
                    "Dữ liệu chữ ký mã hóa không hợp lệ!"
                );
            }


            byte[] iv =
                new byte[12];


            byte[] tag =
                new byte[16];


            byte[] cipherText =
                new byte[
                    duLieu.Length -
                    12 -
                    16
                ];


            Buffer.BlockCopy(
                duLieu,
                0,
                iv,
                0,
                12
            );


            Buffer.BlockCopy(
                duLieu,
                12,
                tag,
                0,
                16
            );


            Buffer.BlockCopy(
                duLieu,
                28,
                cipherText,
                0,
                cipherText.Length
            );


            byte[] plainText =
                new byte[
                    cipherText.Length
                ];


            using AesGcm aes =
                new AesGcm(
                    key,
                    16
                );


            aes.Decrypt(
                iv,
                cipherText,
                tag,
                plainText
            );


            return Convert.ToBase64String(
                plainText
            );
        }


        // =========================================================
        // TẠO KHÓA AES 256-BIT
        // =========================================================

        private byte[] TaoKhoaAES(
            string khoaBiMat)
        {
            using SHA256 sha256 =
                SHA256.Create();


            return sha256.ComputeHash(
                Encoding.UTF8.GetBytes(
                    khoaBiMat
                )
            );
        }
    }
}