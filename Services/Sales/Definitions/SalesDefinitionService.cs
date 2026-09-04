using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Configuration;

namespace GranitWebApi.Services.Sales.Definitions
{
    public class SalesDefinitionService : ISalesDefinitionService
    {
        private readonly IConfiguration _configuration;

        public SalesDefinitionService(
            IConfiguration configuration)
        {
            _configuration = configuration;
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
                ORDER BY URUN_GRUBU";

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

        // =========================================================
        // CK RENKLERİ
        // =========================================================
        //
        // 1) Sadece katalogKodu:
        //    AYAK_RAL listesi
        //
        // 2) katalogKodu + ayakRal:
        //    GOVDE_KANAT_RAL listesi
        //
        // 3) katalogKodu + ayakRal + govdeKanatRal:
        //    Plastik renk numaraları
        //
        // =========================================================

        public async Task<IEnumerable<object>> GetCKProductDetailColorsAsync(string? katalogKodu,string? ayakRal = null,string? govdeKanatRal = null)
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
                cmd.Parameters.AddWithValue("@KatalogKodu",(object?)katalogKodu ?? DBNull.Value);
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
                cmd.Parameters.AddWithValue("@KatalogKodu",(object?)katalogKodu ?? DBNull.Value);
                cmd.Parameters.AddWithValue("@AyakRal",(object?)ayakRal ?? DBNull.Value);
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

            using SqlCommand plasticCmd = new SqlCommand(plasticQuery,conn);
            plasticCmd.Parameters.AddWithValue("@KatalogKodu",(object?)katalogKodu ?? DBNull.Value);
            plasticCmd.Parameters.AddWithValue("@AyakRal",(object?)ayakRal ?? DBNull.Value);
            plasticCmd.Parameters.AddWithValue("@GovdeKanatRal",(object?)govdeKanatRal ?? DBNull.Value);
            using SqlDataReader plasticReader =await plasticCmd.ExecuteReaderAsync();

            while (await plasticReader.ReadAsync())
            {
                result.Add(new
                {
                    plasticColor1No =plasticReader["PLASTIK_RENK_1_NO"]?.ToString()?.Trim(),
                    plasticColor2No =plasticReader["PLASTIK_RENK_2_NO"]?.ToString()?.Trim(),
                    ralKodu =plasticReader["RAL_KODU"]?.ToString()?.Trim(),
                    renkKodu =plasticReader["RENK_KODU"]?.ToString()?.Trim()
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
    }
}