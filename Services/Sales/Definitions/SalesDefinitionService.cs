using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace GranitWebApi.Services.Sales.Definitions
{
    public class SalesDefinitionService : ISalesDefinitionService
    {
        private readonly IConfiguration _configuration;
        private readonly ErpDbContext _erpContext;

        public SalesDefinitionService(IConfiguration configuration, ErpDbContext erpContext)
        {
            _configuration = configuration;
            _erpContext = erpContext;
        }

        private string ConnectionString => _configuration.GetConnectionString("DefaultConnection")!;
        public async Task<IEnumerable<object>> GetProductGroupsAsync()
        {
            var result = new List<object>();
            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string query = @"
                SELECT
                    ID,
                    URUN_GRUBU,
                    KISATLMASI
                FROM GRANIT_TBL_URUN_GRUBU
                ORDER BY ID";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    id = reader["ID"]?.ToString(),
                    urunGrubu = reader["URUN_GRUBU"]?.ToString()?.Trim(),
                    kisaltmasi = reader["KISATLMASI"]?.ToString()?.Trim()
                });
            }

            return result;
        }

        // ÜRÜN TİPLERİ
        public async Task<IEnumerable<object>> GetProductTypesAsync(string? urunGrupId)
        {
            var result = new List<object>();
            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string query = @"
                SELECT
                    URUN_GRUP_ID,
                    URUN_TIPI_ID,
                    URUN_TIPI
                FROM GRANIT_TBL_URUN_TIPI
                WHERE URUN_GRUP_ID = @UrunGrupId
                ORDER BY URUN_TIPI_ID";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@UrunGrupId", (object?)urunGrupId ?? DBNull.Value);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    urunGrupId = reader["URUN_GRUP_ID"]?.ToString()?.Trim(),
                    urunTipiId = reader["URUN_TIPI_ID"]?.ToString()?.Trim(),
                    urunTipi = reader["URUN_TIPI"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // CK ÜRÜNLERİ
        public async Task<IEnumerable<object>> GetCKProductDetailsAsync(string? urunGrupId, string? urunTipiId)
        {
            var result = new List<object>();
            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string query = @"
                SELECT
                    URUN_ID,
                    URUN_GRUP_ID,
                    URUN_TIPI_ID,
                    URUN_ADI,
                    KATALOG_KODU,
                    GOVDE_DETAY,
                    KANAT_DETAY
                FROM GRANIT_TBL_CK_URUN_DETAY_ANA
                WHERE URUN_GRUP_ID = @UrunGrupId
                  AND URUN_TIPI_ID = @UrunTipiId
                ORDER BY URUN_ADI";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@UrunGrupId", (object?)urunGrupId ?? DBNull.Value);
            cmd.Parameters.AddWithValue("@UrunTipiId", (object?)urunTipiId ?? DBNull.Value);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    urunId = reader["URUN_ID"]?.ToString()?.Trim(),
                    urunGrupId = reader["URUN_GRUP_ID"]?.ToString()?.Trim(),
                    urunTipiId = reader["URUN_TIPI_ID"]?.ToString()?.Trim(),
                    urunAdi = reader["URUN_ADI"]?.ToString()?.Trim(),
                    katalogKodu = reader["KATALOG_KODU"]?.ToString()?.Trim(),
                    govdeDetay = reader["GOVDE_DETAY"]?.ToString()?.Trim(),
                    kanatDetay = reader["KANAT_DETAY"]?.ToString()?.Trim()
                });
            }
            return result;
        }
        public async Task<IEnumerable<object>> GetCKProductDetailColorsAsync(string? katalogKodu, string? ayakRal = null, string? govdeKanatRal = null)
        {
            var result = new List<object>();
            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            // → AYAK RENKLERİ
            if (string.IsNullOrWhiteSpace(ayakRal))
            {
                const string query = @"
                    SELECT DISTINCT
                        AYAK_RAL,
                        AYAK_RAL_DETAY
                    FROM GRANIT_TBL_CK_URUN_DETAY_RENK
                    WHERE KATALOG_KODU = @KatalogKodu
                    ORDER BY AYAK_RAL_DETAY";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@KatalogKodu", (object?)katalogKodu ?? DBNull.Value);
                using SqlDataReader reader = await cmd.ExecuteReaderAsync();
                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        ayakRal = reader["AYAK_RAL"]?.ToString()?.Trim(),
                        ayakRalDetay = reader["AYAK_RAL_DETAY"]?.ToString()?.Trim()
                    });
                }
                return result;
            }
            // → GÖVDE / KANAT RENKLERİ

            if (string.IsNullOrWhiteSpace(govdeKanatRal))
            {
                const string query = @"
                    SELECT DISTINCT
                        GOVDE_KANAT_RAL,
                        GOVDE_KANAT_RAL_DETAY
                    FROM GRANIT_TBL_CK_URUN_DETAY_RENK
                    WHERE KATALOG_KODU = @KatalogKodu
                      AND AYAK_RAL = @AyakRal
                    ORDER BY GOVDE_KANAT_RAL_DETAY";

                using SqlCommand cmd = new SqlCommand(query, conn);
                cmd.Parameters.AddWithValue("@KatalogKodu", (object?)katalogKodu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AyakRal", (object?)ayakRal ?? DBNull.Value);
                using SqlDataReader reader = await cmd.ExecuteReaderAsync();

                while (await reader.ReadAsync())
                {
                    result.Add(new
                    {
                        govdeKanatRal = reader["GOVDE_KANAT_RAL"]?.ToString()?.Trim(),
                        govdeKanatRalDetay = reader["GOVDE_KANAT_RAL_DETAY"]?.ToString()?.Trim()
                    });
                }
                return result;
            }
            // → PLASTİK RENK KOMBİNASYONLARI

            const string plasticQuery = @"
                SELECT DISTINCT
                    DR.PLASTIK_RENK_1_NO,
                    DR.PLASTIK_RENK_2_NO,
                    DR.PLASTIK_RENK_1_NO + '-' +
                    DR.PLASTIK_RENK_2_NO AS RAL_KODU,
                    CASE
                        WHEN DR.PLASTIK_RENK_1_NO <> DR.PLASTIK_RENK_2_NO
                            THEN PR1.RENK + '-' + PR2.RENK
                        ELSE PR1.RENK
                    END AS RENK_KODU
                FROM GRANIT_TBL_CK_URUN_DETAY_RENK DR
                INNER JOIN GRANIT_TBL_PLASTIK_RENK PR1 WITH(NOLOCK)
                    ON DR.PLASTIK_RENK_1_NO = PR1.NO
                INNER JOIN GRANIT_TBL_PLASTIK_RENK PR2 WITH(NOLOCK)
                    ON DR.PLASTIK_RENK_2_NO = PR2.NO
                WHERE DR.KATALOG_KODU = @KatalogKodu
                  AND DR.AYAK_RAL = @AyakRal
                  AND DR.GOVDE_KANAT_RAL = @GovdeKanatRal
                ORDER BY RAL_KODU";

            using SqlCommand plasticCmd = new SqlCommand(plasticQuery, conn);
            plasticCmd.Parameters.AddWithValue("@KatalogKodu", (object?)katalogKodu ?? DBNull.Value);
            plasticCmd.Parameters.AddWithValue("@AyakRal", (object?)ayakRal ?? DBNull.Value);
            plasticCmd.Parameters.AddWithValue("@GovdeKanatRal", (object?)govdeKanatRal ?? DBNull.Value);
            using SqlDataReader plasticReader = await plasticCmd.ExecuteReaderAsync();

            while (await plasticReader.ReadAsync())
            {
                result.Add(new
                {
                    plasticColor1No = plasticReader["PLASTIK_RENK_1_NO"]?.ToString()?.Trim(),
                    plasticColor2No = plasticReader["PLASTIK_RENK_2_NO"]?.ToString()?.Trim(),
                    ralKodu = plasticReader["RAL_KODU"]?.ToString()?.Trim(),
                    renkKodu = plasticReader["RENK_KODU"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // PLASTİK RENKLERİ
        public async Task<IEnumerable<object>> GetPlasticColorsAsync()
        {
            var result = new List<object>();
            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();
            const string query = @"
                SELECT
                    NO,
                    RENK
                FROM GRANIT_TBL_PLASTIK_RENK
                ORDER BY NO";

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    no = reader["NO"]?.ToString()?.Trim(),
                    renk = reader["RENK"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // GÖRSEL TİPLERİ
        public async Task<IEnumerable<object>> GetSalesOrderImageTypesAsync()
        {
            const string query = @"
                SELECT
                    Id,
                    Code,
                    Name,
                    IsActive,
                    DisplayOrder
                FROM SalesOrderImageTypes
                WHERE IsActive = 1
                ORDER BY DisplayOrder";

            var result = new List<object>();

            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            using SqlCommand cmd = new SqlCommand(query, conn);
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    id = Convert.ToInt32(reader["Id"]),
                    code = reader["Code"]?.ToString()?.Trim(),
                    name = reader["Name"]?.ToString()?.Trim(),
                    isActive = Convert.ToBoolean(reader["IsActive"]),
                    displayOrder = Convert.ToInt32(reader["DisplayOrder"])
                });
            }
            return result;
        }
        // =========================
        // UM GÖVDE TİPLERİ
        // =========================
        public async Task<IEnumerable<object>> GetUMBodyTypesAsync()
        {
            return new List<object>{
                new{code = "MT",name = "Monoblok"},
                new{code = "PT",name = "Perfore"}};
        }

        // UM GÖVDELERİ
        public async Task<IEnumerable<object>> GetUMBodiesAsync(string? bodyType)
        {
            var result = new List<object>();

            if (string.IsNullOrWhiteSpace(bodyType)) return result;

            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string query = @"
                SELECT DISTINCT
                    GOVDE_KODU
                FROM GRANIT_TBL_UM_URUN_DETAY_ANA
                WHERE GOVDE_KODU IS NOT NULL
                  AND GOVDE_KODU LIKE @Prefix + '%'
                ORDER BY GOVDE_KODU";

            using SqlCommand cmd = new SqlCommand(query, conn);
            string prefix = bodyType.Trim().ToUpper() switch
            {
                "MT" => "MT",
                "PT" => "PT",
                "MONOBLOK" => "MT",
                "PERFORE" => "PT",
                _ => ""
            };

            if (string.IsNullOrWhiteSpace(prefix)) return result;
            cmd.Parameters.AddWithValue("@Prefix", prefix);

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    code = reader["GOVDE_KODU"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // UM ÜTÜLÜKLER
        public async Task<IEnumerable<object>> GetUMIroningBoardsAsync(string? bodyCode)
        {
            var result = new List<object>();

            if (string.IsNullOrWhiteSpace(bodyCode)) return result;
            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string query = @"
                SELECT DISTINCT
                    UTULUK_KODU
                FROM GRANIT_TBL_UM_URUN_DETAY_ANA
                WHERE GOVDE_KODU = @GovdeKodu
                  AND UTULUK_KODU IS NOT NULL
                ORDER BY UTULUK_KODU";

            using SqlCommand cmd = new SqlCommand(query, conn);
            cmd.Parameters.AddWithValue("@GovdeKodu", bodyCode.Trim());
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    code = reader["UTULUK_KODU"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // UM AYAKLAR
        public async Task<IEnumerable<object>> GetUMFeetAsync(string? bodyCode, string? ironingBoardCode)
        {
            var result = new List<object>();
            if (string.IsNullOrWhiteSpace(bodyCode) || string.IsNullOrWhiteSpace(ironingBoardCode))
            {
                return result;
            }

            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string query = @"
                SELECT DISTINCT
                    AYAK_KODU
                FROM GRANIT_TBL_UM_URUN_DETAY_ANA
                WHERE GOVDE_KODU = @GovdeKodu
                  AND UTULUK_KODU = @UtulukKodu
                  AND AYAK_KODU IS NOT NULL
                ORDER BY AYAK_KODU";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@GovdeKodu", bodyCode.Trim());
            cmd.Parameters.AddWithValue("@UtulukKodu", ironingBoardCode.Trim());

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    code = reader["AYAK_KODU"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // UM ÜRÜN BİLGİSİ
        public async Task<object?> GetUMProductAsync(string? bodyCode, string? ironingBoardCode, string? footCode)
        {
            if (string.IsNullOrWhiteSpace(bodyCode) || string.IsNullOrWhiteSpace(ironingBoardCode) ||
                string.IsNullOrWhiteSpace(footCode))
            {
                return null;
            }

            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string query = @"
                SELECT TOP 1
                    KATALOG_KODU,
                    URUN_ADI,
                    GOVDE_KODU,
                    UTULUK_KODU,
                    AYAK_KODU,
                    G_U_NO,
                    AYAK_NO,
                    GOVDE_EN,
                    GOVDE_BOY,
                    SUNGER_EN,
                    SUNGER_BOY,
                    KUMAS_EN,
                    KUMAS_BOY,
                    ANTENLI,
                    ANTEN_KOD
                FROM GRANIT_TBL_UM_URUN_DETAY_ANA
                WHERE GOVDE_KODU = @GovdeKodu
                  AND UTULUK_KODU = @UtulukKodu
                  AND AYAK_KODU = @AyakKodu";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@GovdeKodu", bodyCode.Trim());
            cmd.Parameters.AddWithValue("@UtulukKodu", ironingBoardCode.Trim());
            cmd.Parameters.AddWithValue("@AyakKodu", footCode.Trim());

            using SqlDataReader reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync()) return null;

            return new
            {
                katalogKodu = reader["KATALOG_KODU"]?.ToString()?.Trim(),
                urunAdi = reader["URUN_ADI"]?.ToString()?.Trim(),
                govdeKodu = reader["GOVDE_KODU"]?.ToString()?.Trim(),
                utulukKodu = reader["UTULUK_KODU"]?.ToString()?.Trim(),
                ayakKodu = reader["AYAK_KODU"]?.ToString()?.Trim(),
                guNo = reader["G_U_NO"]?.ToString()?.Trim(),
                ayakNo = reader["AYAK_NO"]?.ToString()?.Trim(),
                govdeEn = reader["GOVDE_EN"]?.ToString()?.Trim(),
                govdeBoy = reader["GOVDE_BOY"]?.ToString()?.Trim(),
                sungerEn = reader["SUNGER_EN"]?.ToString()?.Trim(),
                sungerBoy = reader["SUNGER_BOY"]?.ToString()?.Trim(),
                kumasEn = reader["KUMAS_EN"]?.ToString()?.Trim(),
                kumasBoy = reader["KUMAS_BOY"]?.ToString()?.Trim(),
                antenli = reader["ANTENLI"]?.ToString()?.Trim(),
                antenKod = reader["ANTEN_KOD"]?.ToString()?.Trim()
            };
        }

        // UM ÜRÜN RENKLERİ
        public async Task<IEnumerable<object>> GetUMProductColorsAsync(string? katalogKodu)
        {
            var result = new List<object>();
            if (string.IsNullOrWhiteSpace(katalogKodu)) return result;
            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string query = @"
                SELECT DISTINCT
                    G_U_RAL,
                    G_U_RENK,
                    AYAK_RAL,
                    AYAK_RENK
                FROM GRANIT_TBL_UM_URUN_DETAY_RENK
                WHERE KATALOG_KOD = @KatalogKodu
                ORDER BY G_U_RAL,AYAK_RAL";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@KatalogKodu", katalogKodu.Trim());
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    bodyIroningRal = reader["G_U_RAL"]?.ToString()?.Trim(),
                    bodyIroningColor = reader["G_U_RENK"]?.ToString()?.Trim(),
                    footRal = reader["AYAK_RAL"]?.ToString()?.Trim(),
                    footColor = reader["AYAK_RENK"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // UM KUMAŞLARI
        public async Task<IEnumerable<object>> GetUMFabricsAsync(string? katalogKodu)
        {
            var result = new List<object>();
            if (string.IsNullOrWhiteSpace(katalogKodu)) return result;

            using var conn = _erpContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

            const string query = @"
                SELECT
                    QQ.KOD,
                    QQ.KUMAS,
                    QQ.MIKTAR
                FROM
                (
                    SELECT
                        ST.STOK_KODU AS KOD,

                        CASE
                            WHEN CHARINDEX('(', ST.STOK_ADI) > 0
                             AND CHARINDEX(')', ST.STOK_ADI) > CHARINDEX('(', ST.STOK_ADI)
                            THEN SUBSTRING(
                                ST.STOK_ADI,
                                CHARINDEX('(', ST.STOK_ADI) + 1,
                                CHARINDEX(')', ST.STOK_ADI)
                                    - CHARINDEX('(', ST.STOK_ADI) - 1
                            )
                            ELSE NULL
                        END AS KUMAS,

                        ISNULL(
                            (
                                SELECT SUM(ISNULL(STHAR_GCMIK, 0))
                                FROM TBLSTHAR WITH(NOLOCK)
                                WHERE STOK_KODU = ST.STOK_KODU
                                  AND STHAR_GCKOD = 'G'
                            ),
                            0
                        )
                        -
                        ISNULL(
                            (
                                SELECT SUM(ISNULL(STHAR_GCMIK, 0))
                                FROM TBLSTHAR WITH(NOLOCK)
                                WHERE STOK_KODU = ST.STOK_KODU
                                  AND STHAR_GCKOD = 'C'
                            ),
                            0
                        ) AS MIKTAR

                    FROM TBLSTSABIT ST

                    WHERE ST.STOK_KODU LIKE '2218.%'
                      AND ST.STOK_KODU NOT LIKE '2218.01.%'

                      AND ST.STOK_KODU LIKE
                            '2218.%' +
                            (
                                SELECT KUMAS_EN COLLATE Turkish_CI_AS
                                FROM GRANITMETAL..GRANIT_TBL_UM_URUN_DETAY_ANA
                                WHERE KATALOG_KODU = @KatalogKodu
                            )
                            + '.' +
                            (
                                SELECT KUMAS_BOY
                                FROM GRANITMETAL..GRANIT_TBL_UM_URUN_DETAY_ANA
                                WHERE KATALOG_KODU = @KatalogKodu
                            )

                    GROUP BY
                        ST.STOK_KODU,
                        ST.STOK_ADI
                ) QQ

                WHERE QQ.KUMAS IS NOT NULL

                ORDER BY QQ.KOD";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;
            var katalogParameter = cmd.CreateParameter();
            katalogParameter.ParameterName = "@KatalogKodu";
            katalogParameter.Value = katalogKodu.Trim();
            cmd.Parameters.Add(katalogParameter);

            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    kod = reader["KOD"]?.ToString()?.Trim(),
                    kumas = reader["KUMAS"]?.ToString()?.Trim(),
                    miktar = reader["MIKTAR"] == DBNull.Value ? 0 : Convert.ToDecimal(reader["MIKTAR"])
                });
            }
            return result;
        }

        // UM SÜNGERLERİ
        public async Task<IEnumerable<object>> GetUMSpongesAsync(string? katalogKodu)
        {
            var result = new List<object>();
            if (string.IsNullOrWhiteSpace(katalogKodu)) return result;
            using var conn = _erpContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

            const string query = @"
                SELECT
                    ST.STOK_KODU AS KOD,
                    SUBSTRING(ST.STOK_KODU, 6, 2) AS ISIM
                FROM TBLSTSABIT ST
                WHERE ST.STOK_KODU LIKE '2217.%'
                  AND ST.STOK_KODU COLLATE SQL_Latin1_General_CP1_CI_AS LIKE
                      '%' +
                      (
                          SELECT
                              SUNGER_EN + '.' + SUNGER_BOY
                          FROM GRANITMETAL..GRANIT_TBL_UM_URUN_DETAY_ANA
                          WHERE KATALOG_KODU = @KatalogKodu
                      )
                GROUP BY ST.STOK_KODU";

            await using var cmd = conn.CreateCommand();
            cmd.CommandText = query;

            var katalogParameter = cmd.CreateParameter();
            katalogParameter.ParameterName = "@KatalogKodu";
            katalogParameter.Value = katalogKodu.Trim();

            cmd.Parameters.Add(katalogParameter);
            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    kod = reader["KOD"]?.ToString()?.Trim(),
                    isim = reader["ISIM"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // UM FİŞ TİPLERİ
        public async Task<IEnumerable<object>> GetUMFisTypesAsync()
        {
            return new List<object>
            {
                new {val = "ALMAN",txt = "ALMAN"},
                new {val = "FRANSIZ",txt = "FRANSIZ"},
                new {val = "INGILIZ",txt = "INGILIZ"}
            };
        }

        // UM FİŞ KODLARI
        public async Task<IEnumerable<object>> GetUMFisCodesAsync(string? fisTip)
        {
            var result = new List<object>();

            if (string.IsNullOrWhiteSpace(fisTip)) return result;

            using var conn = _erpContext.Database.GetDbConnection();
            if (conn.State != System.Data.ConnectionState.Open) await conn.OpenAsync();

            const string query = @"
                SELECT
                    STOK_KODU,
                    STOK_ADI
                FROM TBLSTSABIT
                WHERE STOK_KODU LIKE '3207.%'
                  AND STOK_ADI LIKE '%' + @FisTip + '%'
                ORDER BY STOK_KODU";

            await using var cmd = conn.CreateCommand();

            cmd.CommandText = query;

            var fisTipParameter = cmd.CreateParameter();
            fisTipParameter.ParameterName = "@FisTip";
            fisTipParameter.Value = fisTip.Trim();

            cmd.Parameters.Add(fisTipParameter);
            await using var reader = await cmd.ExecuteReaderAsync();
            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    kod = reader["STOK_KODU"]?.ToString()?.Trim(),
                    ad = reader["STOK_ADI"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // UM PLASTİK RENK KOMBİNASYONLARI
        public async Task<IEnumerable<object>> GetUMPlasticCombinationsAsync(string? kumasKod)
        {
            var result = new List<object>();

            if (string.IsNullOrWhiteSpace(kumasKod)) return result;
            using SqlConnection conn = new SqlConnection(ConnectionString);
            await conn.OpenAsync();

            const string query = @"
                SELECT DISTINCT
                    KUMAS_KOD,
                    KOMBIN_NO,
                    KOMBIN_ACIKLAMA
                FROM GRANIT_TBL_URUN_KUMAS_PLASTIK_RENK
                WHERE KUMAS_KOD = @KumasKod
                ORDER BY KOMBIN_NO";

            using SqlCommand cmd = new SqlCommand(query, conn);

            cmd.Parameters.AddWithValue("@KumasKod", kumasKod.Trim());
            using SqlDataReader reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    kumasKod = reader["KUMAS_KOD"]?.ToString()?.Trim(),
                    kombinNo = reader["KOMBIN_NO"]?.ToString()?.Trim(),
                    kombinAciklama = reader["KOMBIN_ACIKLAMA"]?.ToString()?.Trim()
                });
            }
            return result;
        }

        // =========================================================
        // YK - YEDEK KILIF TÜRLERİ
        // =========================================================
        public async Task<IEnumerable<object>> GetYKTypesAsync()
        {
            var result = new List<object>();

            using var conn = _erpContext.Database.GetDbConnection();

            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            const string query = @"
        SELECT
            STOK_KODU AS VAL,
            STOK_ADI AS TXTE
        FROM TBLSTSABIT
        WHERE STOK_KODU LIKE '15%'
          AND STOK_KODU NOT LIKE '%.%'
        ORDER BY STOK_KODU";

            await using var cmd = conn.CreateCommand();

            cmd.CommandText = query;

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    val = reader["VAL"]?.ToString()?.Trim(),
                    txte = reader["TXTE"]?.ToString()?.Trim()
                });
            }

            return result;
        }


        // =========================================================
        // YK - YEDEK KILIF KUMAŞLARI
        // =========================================================
        public async Task<IEnumerable<object>> GetYKFabricsAsync(
            string? katalogKodu)
        {
            var result = new List<object>();

            if (string.IsNullOrWhiteSpace(katalogKodu))
                return result;

            using var conn = _erpContext.Database.GetDbConnection();

            if (conn.State != System.Data.ConnectionState.Open)
                await conn.OpenAsync();

            const string query = @"
        SELECT
            KOD,
            KUMAS,
            STOK_ADI AS STOK_ADI
        FROM
        (
            SELECT
                ST.STOK_KODU AS KOD,

                CASE
                    WHEN CHARINDEX('(', ST.STOK_ADI) > 0
                     AND CHARINDEX(')', ST.STOK_ADI)
                         > CHARINDEX('(', ST.STOK_ADI)
                    THEN SUBSTRING(
                        ST.STOK_ADI,
                        CHARINDEX('(', ST.STOK_ADI) + 1,
                        CHARINDEX(')', ST.STOK_ADI)
                            - CHARINDEX('(', ST.STOK_ADI) - 1
                    )
                    ELSE NULL
                END AS KUMAS,

                ST.STOK_ADI

            FROM TBLSTSABIT ST

            WHERE ST.STOK_KODU LIKE '3105%'

              AND ST.STOK_KODU NOT IN
              (
                  '3105.22.3140.2420',
                  '3105.22.3160.2420',
                  '3105.22.3170.2420',
                  '3105.22.3200.2400',
                  '3105.22.3210.2400',
                  '3105.22.3220.2400',
                  '3105.22.3230.2400',
                  '3105.22.2780.2400',
                  '3105.22.2850.2400',
                  '3105.22.2930.2400'
              )

              AND ST.STOK_ADI NOT LIKE '%PANO%'
        ) QQ

        WHERE @KatalogKodu NOT IN ('15213', '15214')

        UNION ALL

        SELECT
            KOD,
            KUMAS,
            STOK_ADI AS STOK_ADI
        FROM
        (
            SELECT
                ST.STOK_KODU AS KOD,

                CASE
                    WHEN CHARINDEX('(', ST.STOK_ADI) > 0
                     AND CHARINDEX(')', ST.STOK_ADI)
                         > CHARINDEX('(', ST.STOK_ADI)
                    THEN SUBSTRING(
                        ST.STOK_ADI,
                        CHARINDEX('(', ST.STOK_ADI) + 1,
                        CHARINDEX(')', ST.STOK_ADI)
                            - CHARINDEX('(', ST.STOK_ADI) - 1
                    )
                    ELSE NULL
                END AS KUMAS,

                ST.STOK_ADI

            FROM TBLSTSABIT ST

            WHERE ST.STOK_KODU LIKE '3105%'
              AND ST.STOK_ADI LIKE '%K1310%'
        ) QQ

        WHERE @KatalogKodu IN ('15213', '15214')

        ORDER BY KOD";

            await using var cmd = conn.CreateCommand();

            cmd.CommandText = query;

            var katalogParameter = cmd.CreateParameter();
            katalogParameter.ParameterName = "@KatalogKodu";
            katalogParameter.Value = katalogKodu.Trim();

            cmd.Parameters.Add(katalogParameter);

            await using var reader = await cmd.ExecuteReaderAsync();

            while (await reader.ReadAsync())
            {
                result.Add(new
                {
                    kod = reader["KOD"]?.ToString()?.Trim(),
                    kumas = reader["KUMAS"]?.ToString()?.Trim(),
                    stokAdi = reader["STOK_ADI"]?.ToString()?.Trim()
                });
            }

            return result;
        }

    }
}