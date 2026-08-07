using GranitWebApi.Data;
using GranitWebApi.Models.Trendyol;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.Net.Http.Headers;
using System.Text;

namespace GranitWebApi.Services.Trendyol
{
    public class TrendyolService : ITrendyolService
    {
        private readonly HttpClient _httpClient;
        private readonly IConfiguration _configuration;

        private readonly AppDbContext _context;

        public TrendyolService(
    HttpClient httpClient,
    IConfiguration configuration,
    AppDbContext context)
        {
            _httpClient = httpClient;
            _configuration = configuration;
            _context = context;
        }

        public async Task<List<TrendyolOrder>> GetOrdersAsync()
        {
            var supplierId = _configuration["Trendyol:SupplierId"];
            var apiKey = _configuration["Trendyol:ApiKey"];
            var apiSecret = _configuration["Trendyol:ApiSecret"];

            var auth =
                Convert.ToBase64String(
                    Encoding.UTF8.GetBytes($"{apiKey}:{apiSecret}")
                );

            _httpClient.DefaultRequestHeaders.Authorization =
                new AuthenticationHeaderValue("Basic", auth);

            _httpClient.DefaultRequestHeaders.Remove("User-Agent");

            _httpClient.DefaultRequestHeaders.Add(
                "User-Agent",
                $"{supplierId} - SelfIntegration"
            );

            var response = await _httpClient.GetAsync(
                $"https://apigw.trendyol.com/integration/order/sellers/{supplierId}/orders?size=200"
            );

            response.EnsureSuccessStatusCode();

            var json =
                await response.Content.ReadAsStringAsync();

            var result =
                JsonConvert.DeserializeObject<TrendyolOrderResponse>(json);

            return result?.Content ?? new List<TrendyolOrder>();
        }

        public async Task ImportOrdersAsync()
        {
            var orders = await GetOrdersAsync();

            foreach (var order in orders)
            {
                using var transaction = await _context.Database.BeginTransactionAsync();

                try
                {
                    // =========================
                    // 1. RAW JSON (HER ZAMAN INSERT)
                    // =========================
                    var rawJson = System.Text.Json.JsonSerializer.Serialize(order);

                    _context.TyOrderRaws.Add(new TyOrderRaw
                    {
                        TRENDYOL_ORDER_ID = order.Id,
                        ORDER_NUMBER = order.OrderNumber,
                        RAW_JSON = rawJson,
                        CREATED_DATE = DateTime.Now
                    });

                    // =========================
                    // 2. HEADER UPSERT
                    // =========================
                    var header = await _context.TyOrderHeaders
                        .FirstOrDefaultAsync(x => x.TRENDYOL_ORDER_ID == order.Id);

                    bool isNew = false;

                    if (header == null)
                    {
                        isNew = true;

                        header = new TyOrderHeader
                        {
                            TRENDYOL_ORDER_ID = order.Id,
                            CREATED_DATE = DateTime.Now
                        };

                        _context.TyOrderHeaders.Add(header);
                    }

                    // UPDATE FIELDS (ALWAYS REFRESH)
                    header.ORDER_NUMBER = order.OrderNumber;
                    header.CUSTOMER_ID = order.CustomerId;
                    header.CUSTOMER_FIRSTNAME = order.CustomerFirstName;
                    header.CUSTOMER_LASTNAME = order.CustomerLastName;
                    header.CUSTOMER_EMAIL = order.CustomerEmail;
                    header.SUPPLIER_ID = order.SupplierId;
                    header.SHIPMENT_PACKAGE_ID = order.ShipmentPackageId;

                    // STATUS UPDATE (CRITICAL PART)
                    if (header.STATUS != order.Status)
                    {
                        _context.TyIntegrationLogs.Add(new TyIntegrationLog
                        {
                            ORDER_NUMBER = order.OrderNumber,
                            LOG_TYPE = "INFO",
                            MESSAGE = $"Status changed: {header.STATUS} → {order.Status}"
                        });
                    }

                    header.STATUS = order.Status;
                    header.SHIPMENT_PACKAGE_STATUS = order.ShipmentPackageStatus;

                    header.CARGO_PROVIDER_NAME = order.CargoProviderName;
                    header.CARGO_TRACKING_NUMBER = order.CargoTrackingNumber?.ToString();
                    header.CARGO_TRACKING_LINK = order.CargoTrackingLink;
                    header.CARGO_SENDER_NUMBER = order.CargoSenderNumber;

                    header.CURRENCY_CODE = order.CurrencyCode;
                    header.PACKAGE_TOTAL_PRICE = order.PackageTotalPrice;
                    header.PACKAGE_GROSS_AMOUNT = order.PackageGrossAmount;
                    header.PACKAGE_SELLER_DISCOUNT = order.PackageSellerDiscount;
                    header.PACKAGE_TY_DISCOUNT = order.PackageTyDiscount;
                    header.PACKAGE_TOTAL_DISCOUNT = order.PackageTotalDiscount;

                    header.INVOICE_LINK = order.InvoiceLink;
                    header.INVOICE_NUMBER = order.InvoiceNumber;
                    header.INVOICE_STATUS = order.InvoiceStatus;

                    header.COMMERCIAL = order.Commercial;
                    header.MICRO = order.Micro;
                    header.FAST_DELIVERY = order.FastDelivery;
                    header.IS_COD = order.IsCod;

                    header.ORDER_DATE = DateTimeOffset
                        .FromUnixTimeMilliseconds(order.OrderDate)
                        .DateTime;

                    header.LAST_MODIFIED_DATE = DateTimeOffset
                        .FromUnixTimeMilliseconds(order.LastModifiedDate)
                        .DateTime;

                    header.INTEGRATION_STATUS = 0;

                    // =========================
                    // 3. LINES (ONLY INSERT ON NEW ORDER)
                    // =========================
                    if (isNew && order.Lines != null)
                    {
                        foreach (var line in order.Lines)
                        {
                            _context.TyOrderLines.Add(new TyOrderLine
                            {
                                ORDER_HEADER_ID = header.ID,
                                LINE_ID = line.LineId,
                                BARCODE = line.Barcode,
                                STOCK_CODE = line.StockCode,
                                PRODUCT_NAME = line.ProductName,
                                PRODUCT_SIZE = line.ProductSize,
                                PRODUCT_COLOR = line.ProductColor,
                                PRODUCT_CATEGORY_ID = line.ProductCategoryId,
                                QUANTITY = line.Quantity,
                                LINE_UNIT_PRICE = line.LineUnitPrice,
                                LINE_GROSS_AMOUNT = line.LineGrossAmount,
                                LINE_SELLER_DISCOUNT = line.LineSellerDiscount,
                                LINE_TY_DISCOUNT = line.LineTyDiscount,
                                LINE_TOTAL_DISCOUNT = line.LineTotalDiscount,
                                VAT_RATE = line.VatRate,
                                COMMISSION = line.Commission,
                                ORDER_LINE_ITEM_STATUS_NAME = line.OrderLineItemStatusName,
                                CANCELLED_BY = line.CancelledBy,
                                CANCEL_REASON = line.CancelReason,
                                CANCEL_REASON_CODE = line.CancelReasonCode
                            });
                        }
                    }

                    // =========================
                    // 4. ADDRESSES (ONLY ON NEW ORDER)
                    // =========================
                    if (isNew)
                    {
                        if (order.ShipmentAddress != null)
                        {
                            _context.TyOrderAddresses.Add(new TyOrderAddress
                            {
                                ORDER_HEADER_ID = header.ID,
                                ADDRESS_TYPE = "SHIPMENT",
                                ADDRESS_ID = order.ShipmentAddress.Id,
                                FIRST_NAME = order.ShipmentAddress.FirstName,
                                LAST_NAME = order.ShipmentAddress.LastName,
                                COMPANY = order.ShipmentAddress.Company,
                                ADDRESS1 = order.ShipmentAddress.Address1,
                                ADDRESS2 = order.ShipmentAddress.Address2,
                                CITY = order.ShipmentAddress.City,
                                CITY_CODE = order.ShipmentAddress.CityCode,
                                DISTRICT = order.ShipmentAddress.District,
                                DISTRICT_ID = order.ShipmentAddress.DistrictId,
                                POSTAL_CODE = order.ShipmentAddress.PostalCode,
                                COUNTRY_CODE = order.ShipmentAddress.CountryCode,
                                NEIGHBORHOOD = order.ShipmentAddress.Neighborhood,
                                PHONE = order.ShipmentAddress.Phone,
                                FULL_ADDRESS = order.ShipmentAddress.FullAddress,
                                FULL_NAME = order.ShipmentAddress.FullName,
                                TAX_OFFICE = order.ShipmentAddress.TaxOffice,
                                TAX_NUMBER = order.ShipmentAddress.TaxNumber,
                                LATITUDE = order.ShipmentAddress.Latitude,
                                LONGITUDE = order.ShipmentAddress.Longitude
                            });
                        }

                        if (order.InvoiceAddress != null)
                        {
                            _context.TyOrderAddresses.Add(new TyOrderAddress
                            {
                                ORDER_HEADER_ID = header.ID,
                                ADDRESS_TYPE = "INVOICE",
                                ADDRESS_ID = order.InvoiceAddress.Id,
                                FIRST_NAME = order.InvoiceAddress.FirstName,
                                LAST_NAME = order.InvoiceAddress.LastName,
                                COMPANY = order.InvoiceAddress.Company,
                                ADDRESS1 = order.InvoiceAddress.Address1,
                                ADDRESS2 = order.InvoiceAddress.Address2,
                                CITY = order.InvoiceAddress.City,
                                CITY_CODE = order.InvoiceAddress.CityCode,
                                DISTRICT = order.InvoiceAddress.District,
                                DISTRICT_ID = order.InvoiceAddress.DistrictId,
                                POSTAL_CODE = order.InvoiceAddress.PostalCode,
                                COUNTRY_CODE = order.InvoiceAddress.CountryCode,
                                NEIGHBORHOOD = order.InvoiceAddress.Neighborhood,
                                PHONE = order.InvoiceAddress.Phone,
                                FULL_ADDRESS = order.InvoiceAddress.FullAddress,
                                FULL_NAME = order.InvoiceAddress.FullName,
                                TAX_OFFICE = order.InvoiceAddress.TaxOffice,
                                TAX_NUMBER = order.InvoiceAddress.TaxNumber,
                                LATITUDE = order.InvoiceAddress.Latitude,
                                LONGITUDE = order.InvoiceAddress.Longitude
                            });
                        }
                    }

                    // =========================
                    // 5. PACKAGE HISTORY (ALWAYS INSERT)
                    // =========================
                    if (order.PackageHistories != null)
                    {
                        foreach (var history in order.PackageHistories)
                        {
                            _context.TyPackageHistories.Add(new TyPackageHistory
                            {
                                ORDER_HEADER_ID = header.ID,
                                STATUS = history.Status,
                                CREATED_DATE = DateTimeOffset
                                    .FromUnixTimeMilliseconds(history.CreatedDate)
                                    .DateTime
                            });
                        }
                    }

                    // =========================
                    // 6. FINAL SAVE
                    // =========================
                    await _context.SaveChangesAsync();
                    await transaction.CommitAsync();

                    // =========================
                    // 7. SUCCESS LOG
                    // =========================
                    _context.TyIntegrationLogs.Add(new TyIntegrationLog
                    {
                        ORDER_NUMBER = order.OrderNumber,
                        LOG_TYPE = "INFO",
                        MESSAGE = isNew ? "Order inserted" : "Order updated"
                    });

                    await _context.SaveChangesAsync();
                }
                catch (Exception ex)
                {
                    await transaction.RollbackAsync();

                    _context.TyIntegrationLogs.Add(new TyIntegrationLog
                    {
                        ORDER_NUMBER = order.OrderNumber,
                        LOG_TYPE = "ERROR",
                        MESSAGE = ex.Message + " | " + ex.InnerException?.Message
                    });

                    await _context.SaveChangesAsync();
                }
            }
        }
    }
}