using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Entities;
using Studiobook_backend.Services.Interfaces;

namespace Studiobook_backend.Seeders
{
    public static class DemoDataSeeder
    {
        public static async Task SeedAsync(IServiceProvider services)
        {
            using var scope = services.CreateScope();

            var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
            var passwordHasher = scope.ServiceProvider.GetRequiredService<IPasswordHasherService>();
            var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>()
                .CreateLogger("DemoDataSeeder");

            try
            {
                await dbContext.Database.MigrateAsync();

                if (await dbContext.Roles.AnyAsync() ||
                    await dbContext.Users.AnyAsync() ||
                    await dbContext.UserRoles.AnyAsync())
                {
                    logger.LogInformation("Seed skipped. Roles, Users, or UserRoles already exist.");
                    return;
                }

                await using var transaction = await dbContext.Database.BeginTransactionAsync();

                var now = DateTime.UtcNow;

                var generalUserRole = new Role { Name = "GeneralUser" };
                var hostRole = new Role { Name = "Host" };
                var adminRole = new Role { Name = "Admin" };

                dbContext.Roles.AddRange(generalUserRole, hostRole, adminRole);
                await dbContext.SaveChangesAsync();

                var generalPasswordHash = passwordHasher.Hash("User1234!");
                var hostPasswordHash = passwordHasher.Hash("Host1234!");

                var generalUser = new User
                {
                    Name = "一般ユーザー",
                    Kana = "イッパンユーザー",
                    Email = "user@example.com",
                    PasswordHash = generalPasswordHash,
                    PostalCode = "100-0001",
                    Address = "東京都千代田区サンプル1-1-1",
                    PhoneNumber = "09012345678",
                    UsageType = "personal",
                    Enabled = true,
                    EmailVerifiedAtUtc = now
                };

                var generalUser2 = new User
                {
                    Name = "一般ユーザー2",
                    Kana = "イッパンユーザーニ",
                    Email = "user2@example.com",
                    PasswordHash = generalPasswordHash,
                    PostalCode = "104-0061",
                    Address = "東京都中央区サンプル2-1-1",
                    PhoneNumber = "09012345679",
                    UsageType = "personal",
                    Enabled = true,
                    EmailVerifiedAtUtc = now
                };

                var generalUser3 = new User
                {
                    Name = "一般ユーザー3",
                    Kana = "イッパンユーザーサン",
                    Email = "user3@example.com",
                    PasswordHash = generalPasswordHash,
                    PostalCode = "113-0033",
                    Address = "東京都文京区サンプル3-1-1",
                    PhoneNumber = "09012345670",
                    UsageType = "personal",
                    Enabled = true,
                    EmailVerifiedAtUtc = now
                };

                var hostUser = new User
                {
                    Name = "ホストユーザー1",
                    Kana = "ホストユーザーイチ",
                    Email = "host@example.com",
                    PasswordHash = hostPasswordHash,
                    PostalCode = "150-0001",
                    Address = "東京都渋谷区サンプル2-2-2",
                    PhoneNumber = "09023456789",
                    UsageType = "business",
                    Enabled = true,
                    EmailVerifiedAtUtc = now
                };

                var hostUser2 = new User
                {
                    Name = "ホストユーザー2",
                    Kana = "ホストユーザーニ",
                    Email = "host2@example.com",
                    PasswordHash = hostPasswordHash,
                    PostalCode = "160-0022",
                    Address = "東京都新宿区サンプル2-3-1",
                    PhoneNumber = "09023456780",
                    UsageType = "business",
                    Enabled = true,
                    EmailVerifiedAtUtc = now
                };

                var hostUser3 = new User
                {
                    Name = "ホストユーザー3",
                    Kana = "ホストユーザーサン",
                    Email = "host3@example.com",
                    PasswordHash = hostPasswordHash,
                    PostalCode = "170-0013",
                    Address = "東京都豊島区サンプル2-4-1",
                    PhoneNumber = "09023456781",
                    UsageType = "business",
                    Enabled = true,
                    EmailVerifiedAtUtc = now
                };

                var adminUser = new User
                {
                    Name = "管理者ユーザー",
                    Kana = "カンリシャユーザー",
                    Email = "admin@example.com",
                    PasswordHash = passwordHasher.Hash("Admin1234!"),
                    PostalCode = "160-0001",
                    Address = "東京都新宿区サンプル3-3-3",
                    PhoneNumber = "09034567890",
                    UsageType = "admin",
                    Enabled = true,
                    EmailVerifiedAtUtc = now
                };

                var disabledUser = new User
                {
                    Name = "無効ユーザー",
                    Kana = "ムコウユーザー",
                    Email = "disabled@example.com",
                    PasswordHash = passwordHasher.Hash("Disabled1234!"),
                    PostalCode = "170-0001",
                    Address = "東京都豊島区サンプル4-4-4",
                    PhoneNumber = "09045678901",
                    UsageType = "personal",
                    Enabled = false,
                    EmailVerifiedAtUtc = null
                };

                dbContext.Users.AddRange(
                    generalUser,
                    generalUser2,
                    generalUser3,
                    hostUser,
                    hostUser2,
                    hostUser3,
                    adminUser,
                    disabledUser
                );

                await dbContext.SaveChangesAsync();

                dbContext.UserRoles.AddRange(
                    new UserRole
                    {
                        UserId = generalUser.Id,
                        RoleId = generalUserRole.Id
                    },
                    new UserRole
                    {
                        UserId = generalUser2.Id,
                        RoleId = generalUserRole.Id
                    },
                    new UserRole
                    {
                        UserId = generalUser3.Id,
                        RoleId = generalUserRole.Id
                    },
                    new UserRole
                    {
                        UserId = hostUser.Id,
                        RoleId = hostRole.Id
                    },
                    new UserRole
                    {
                        UserId = hostUser2.Id,
                        RoleId = hostRole.Id
                    },
                    new UserRole
                    {
                        UserId = hostUser3.Id,
                        RoleId = hostRole.Id
                    },
                    new UserRole
                    {
                        UserId = adminUser.Id,
                        RoleId = adminRole.Id
                    },
                    new UserRole
                    {
                        UserId = disabledUser.Id,
                        RoleId = generalUserRole.Id
                    }
                );

                await dbContext.SaveChangesAsync();

                var roomSeeds = new List<RoomSeed>
                {
                    new(1,  "千代田区 皇居前ワークスタジオ",       "千代田区の落ち着いたエリアにある、会議・配信・少人数レッスン向けの多目的スタジオです。", 4800, 10, "100-0001", "東京都千代田区サンプル"),
                    new(2,  "中央区 銀座フォトスタジオ",           "銀座エリアを想定した、商品撮影やポートレート撮影に使いやすい明るい撮影スタジオです。", 6500, 8,  "104-0061", "東京都中央区サンプル"),
                    new(3,  "港区 赤坂プレミアムスタジオ",         "赤坂エリアの上質な雰囲気をイメージした、撮影・配信・商談に対応できるスタジオです。", 7200, 12, "107-0052", "東京都港区サンプル"),
                    new(4,  "新宿区 西新宿レッスンスタジオ",       "西新宿エリアを想定した、ダンス・ヨガ・セミナー利用に適した広めのレッスンスタジオです。", 5200, 16, "160-0023", "東京都新宿区サンプル"),
                    new(5,  "文京区 本郷セミナールーム",           "本郷エリアを想定した、勉強会やワークショップに使いやすい静かなセミナールームです。", 3800, 14, "113-0033", "東京都文京区サンプル"),
                    new(6,  "台東区 上野クリエイティブルーム",     "上野エリアを想定した、動画撮影やハンドメイド教室に使いやすいカジュアルなスペースです。", 3600, 10, "110-0005", "東京都台東区サンプル"),
                    new(7,  "墨田区 押上ダンススタジオ",           "押上エリアを想定した、鏡付きのダンス練習や軽い運動に利用しやすいスタジオです。", 4300, 15, "131-0045", "東京都墨田区サンプル"),
                    new(8,  "江東区 豊洲ベイサイドスタジオ",       "豊洲エリアを想定した、配信・撮影・少人数イベントに向いた開放感のあるスタジオです。", 5800, 18, "135-0061", "東京都江東区サンプル"),
                    new(9,  "品川区 五反田ミーティングスタジオ",   "五反田エリアを想定した、商談・面談・オンライン会議に使いやすいビジネス向けスタジオです。", 4200, 8,  "141-0022", "東京都品川区サンプル"),
                    new(10, "目黒区 中目黒ナチュラルスタジオ",     "中目黒エリアを想定した、自然光撮影やヨガレッスンに適した落ち着いたスタジオです。", 6200, 12, "153-0051", "東京都目黒区サンプル"),
                    new(11, "大田区 蒲田マルチスタジオ",           "蒲田エリアを想定した、音楽練習・動画撮影・教室利用に対応する多目的スタジオです。", 3900, 12, "144-0052", "東京都大田区サンプル"),
                    new(12, "世田谷区 三軒茶屋ヨガスタジオ",       "三軒茶屋エリアを想定した、ヨガやピラティスなど少人数レッスンに向いた明るいスタジオです。", 5000, 14, "154-0024", "東京都世田谷区サンプル"),

                    new(13, "渋谷区 表参道ポートレートスタジオ",   "表参道エリアを想定した、プロフィール撮影やSNS用撮影に使いやすいデザイン性のあるスタジオです。", 7000, 8,  "150-0001", "東京都渋谷区サンプル"),
                    new(14, "中野区 中野ブロードスタジオ",         "中野エリアを想定した、配信・収録・ワークショップに対応する扱いやすいスタジオです。", 4100, 10, "164-0001", "東京都中野区サンプル"),
                    new(15, "杉並区 高円寺アートスタジオ",         "高円寺エリアを想定した、アート制作や小規模展示、撮影に使える個性的なスタジオです。", 4500, 10, "166-0003", "東京都杉並区サンプル"),
                    new(16, "豊島区 池袋フォトラウンジ",           "池袋エリアを想定した、ポートレート撮影や商品撮影に利用しやすいフォトスタジオです。", 5600, 9,  "170-0013", "東京都豊島区サンプル"),
                    new(17, "北区 赤羽イベントスタジオ",           "赤羽エリアを想定した、少人数イベントやレッスン、交流会に使いやすいスタジオです。", 4000, 18, "115-0045", "東京都北区サンプル"),
                    new(18, "荒川区 日暮里クラフトルーム",         "日暮里エリアを想定した、手芸教室やワークショップに適した落ち着いたクラフトルームです。", 3500, 8,  "116-0013", "東京都荒川区サンプル"),
                    new(19, "板橋区 成増トレーニングスタジオ",     "成増エリアを想定した、軽運動やパーソナルトレーニングに使いやすいスタジオです。", 4200, 10, "175-0094", "東京都板橋区サンプル"),
                    new(20, "練馬区 大泉ミュージックスタジオ",     "大泉エリアを想定した、楽器練習やボーカル練習、簡易収録に使える音楽スタジオです。", 4600, 6,  "178-0063", "東京都練馬区サンプル"),
                    new(21, "足立区 北千住レンタルスタジオ",       "北千住エリアを想定した、ダンス練習や撮影、教室利用に対応したレンタルスタジオです。", 3800, 15, "120-0034", "東京都足立区サンプル"),
                    new(22, "葛飾区 亀有コミュニティスタジオ",     "亀有エリアを想定した、地域向け教室や小規模イベントに使いやすいスタジオです。", 3300, 16, "125-0061", "東京都葛飾区サンプル"),
                    new(23, "江戸川区 葛西ファミリースタジオ",     "葛西エリアを想定した、親子向け教室や撮影、ワークショップに利用しやすいスタジオです。", 3700, 12, "134-0083", "東京都江戸川区サンプル"),
                };

                var rooms = roomSeeds.Select(seed => new Room
                {
                    UserId = seed.Number <= 12 ? hostUser.Id : hostUser2.Id,
                    Name = seed.Name,
                    ImageName = $"room{seed.Number:00}.jpg",
                    Description = seed.Description,
                    Price = seed.Price,
                    Capacity = seed.Capacity,
                    PostalCode = seed.PostalCode,
                    Address = seed.Address,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }).ToList();

                dbContext.Rooms.AddRange(rooms);
                await dbContext.SaveChangesAsync();

                var businessHours = new List<BusinessHour>();

                foreach (var room in rooms)
                {
                    for (var day = 1; day <= 7; day++)
                    {
                        var isHoliday = day is 6 or 7;

                        businessHours.Add(new BusinessHour
                        {
                            RoomId = room.Id,
                            DayOfWeek = day,
                            StartTime = isHoliday ? null : new TimeOnly(9, 0),
                            EndTime = isHoliday ? null : new TimeOnly(18, 0),
                            IsHoliday = isHoliday,
                            CreatedAtUtc = now,
                            UpdatedAtUtc = now
                        });
                    }
                }

                dbContext.BusinessHours.AddRange(businessHours);
                await dbContext.SaveChangesAsync();

                var priceRules = rooms.Select(room => new PriceRule
                {
                    RoomId = room.Id,
                    RuleType = "multiplier",
                    Weekday = null,
                    StartHour = new TimeOnly(18, 0),
                    EndHour = new TimeOnly(21, 0),
                    Multiplier = 1.50m,
                    FlatFee = null,
                    Note = "夜間料金",
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                }).ToList();

                dbContext.PriceRules.AddRange(priceRules);
                await dbContext.SaveChangesAsync();


                // 予約データを生成
                // 予約データを生成
                var orderedRooms = rooms.OrderBy(x => x.Id).ToList();

                var reservations = new List<Reservation>();

                var reservationStartDate = new DateTime(2026, 3, 1);
                var reservationEndDate = new DateTime(2027, 3, 31);

                var guestUsers = new[]
                {
    generalUser,
    generalUser2
};

                var reservationIndex = 0;

                for (var date = reservationStartDate.Date;
                     date <= reservationEndDate.Date;
                     date = date.AddDays(2))
                {
                    if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday)
                    {
                        continue;
                    }

                    var room = orderedRooms[reservationIndex % orderedRooms.Count];
                    var guest = guestUsers[reservationIndex % guestUsers.Length];

                    var startAt = date.AddHours(9);
                    var endAt = date.AddHours(20);

                    var amount = CalculateDemoReservationTotalAmount(
                        hourlyPrice: room.Price,
                        startAt: startAt,
                        endAt: endAt
                    );

                    var status = (reservationIndex % 5) switch
                    {
                        0 => "booked",
                        1 => "paid",
                        2 => "booked",
                        3 => "canceled",
                        _ => "paid"
                    };

                    reservations.Add(new Reservation
                    {
                        RoomId = room.Id,
                        UserId = guest.Id,
                        StartAt = startAt,
                        EndAt = endAt,
                        Amount = amount,
                        Status = status,
                        CreatedAtUtc = now,
                        UpdatedAtUtc = now
                    });

                    reservationIndex++;
                }

                dbContext.Reservations.AddRange(reservations);
                await dbContext.SaveChangesAsync();

                // 予約料金明細データを生成
                var reservationChargeItems = new List<ReservationChargeItem>();

                var roomsById = rooms.ToDictionary(x => x.Id);

                foreach (var reservation in reservations)
                {
                    var room = roomsById[reservation.RoomId];

                    reservationChargeItems.AddRange(
                        BuildDemoReservationChargeItems(
                            reservation: reservation,
                            hourlyPrice: room.Price
                        )
                    );
                }

                dbContext.ReservationChargeItems.AddRange(reservationChargeItems);
                await dbContext.SaveChangesAsync();

                // レビューデータを生成
                var reviews = new List<Review>();

                var reviewUsers = new[]
                {
    generalUser,
    generalUser2
};

                var reviewTemplates = new[]
                {
    new
    {
        Score = 5,
        Contents = new[]
        {
            "設備がとてもきれいで、入退室もスムーズでした。撮影にも集中でき、また利用したいと思えるスタジオでした。",
            "広さ・明るさ・清潔感のバランスが良く、安心して利用できました。案内も分かりやすかったです。",
            "初めての利用でしたが、室内が整っていて使いやすかったです。料金にも納得感がありました。"
        },
        PublicVisible = true,
        HiddenReason = (string?)null,
        HostReply = "ご利用ありがとうございました。快適にお使いいただけたようで安心しました。またのご利用をお待ちしております。"
    },
    new
    {
        Score = 4,
        Contents = new[]
        {
            "駅から比較的近く、少人数での利用にはちょうど良いスタジオでした。全体的に使いやすかったです。",
            "撮影や練習に必要なスペースは十分ありました。室内も整理されていて、問題なく利用できました。",
            "料金体系が分かりやすく、予約時間どおりに利用できました。機会があればまた使いたいです。"
        },
        PublicVisible = true,
        HiddenReason = (string?)null,
        HostReply = (string?)null
    },
    new
    {
        Score = 4,
        Contents = new[]
        {
            "室内は清潔で、レッスン用途にも使いやすい印象でした。備品の配置も分かりやすかったです。",
            "落ち着いた雰囲気で、打ち合わせや簡単な撮影に向いていると感じました。",
            "利用までの流れが分かりやすく、当日も迷わず使えました。全体的に満足しています。"
        },
        PublicVisible = true,
        HiddenReason = (string?)null,
        HostReply = "レビューありがとうございます。今後も使いやすいスタジオ運営を心がけてまいります。"
    },
    new
    {
        Score = 3,
        Contents = new[]
        {
            "利用自体は問題ありませんでしたが、時間帯によっては周辺の音が少し気になりました。",
            "広さは十分でしたが、もう少し案内表示が分かりやすいとさらに使いやすいと感じました。",
            "基本的には問題なく利用できましたが、一部設備の場所が分かりにくかったです。"
        },
        PublicVisible = false,
        HiddenReason = (string?)"内容確認中のため一時的に非公開",
        HostReply = (string?)null
    }
};

                var orderedRoomsForReviews = rooms.OrderBy(x => x.Id).ToList();

                var reviewIndex = 0;

                foreach (var room in orderedRoomsForReviews)
                {
                    for (var i = 0; i < 4; i++)
                    {
                        var template = reviewTemplates[i];
                        var reviewer = reviewUsers[reviewIndex % reviewUsers.Length];

                        var content = template.Contents[
                            reviewIndex % template.Contents.Length
                        ];

                        var createdAt = new DateTime(2026, 4, 1)
                            .AddDays(reviewIndex * 3)
                            .AddHours(10 + (i % 6));

                        reviews.Add(new Review
                        {
                            RoomId = room.Id,
                            UserId = reviewer.Id,
                            Score = template.Score,
                            Content = content,
                            PublicVisible = template.PublicVisible,
                            HiddenReason = template.HiddenReason,
                            HostReply = template.HostReply,
                            HostReplyAt = template.HostReply == null ? null : createdAt.AddDays(1),
                            CreatedAtUtc = createdAt,
                            UpdatedAtUtc = template.HostReply == null ? createdAt : createdAt.AddDays(1)
                        });

                        reviewIndex++;
                    }
                }

                dbContext.Reviews.AddRange(reviews);
                await dbContext.SaveChangesAsync();

                await SeedDemoAuditLogsAsync(dbContext, logger);

                await transaction.CommitAsync();

                logger.LogInformation("Demo seed completed.");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Demo seed failed.");
                throw;
            }
        }

        private sealed record RoomSeed(
            int Number,
            string Name,
            string Description,
            int Price,
            int Capacity,
            string PostalCode,
            string Address
        );

        private const decimal TaxRate = 0.10m;
        private const decimal PlatformFeeRate = 0.10m;
        private const decimal NightMultiplier = 1.50m;

        private static int CalculateDemoReservationBaseAmount(
            int hourlyPrice,
            DateTime startAt,
            DateTime endAt)
        {
            decimal total = 0m;

            var current = startAt;

            while (current < endAt)
            {
                var next = current.AddHours(1);

                if (next > endAt)
                {
                    next = endAt;
                }

                var hours = (decimal)(next - current).TotalHours;

                var multiplier = current.TimeOfDay >= new TimeSpan(18, 0, 0)
                    ? NightMultiplier
                    : 1.0m;

                total += hourlyPrice * hours * multiplier;

                current = next;
            }

            return RoundYen(total);
        }

        private static int CalculateDemoReservationTotalAmount(
            int hourlyPrice,
            DateTime startAt,
            DateTime endAt)
        {
            var baseAmount = CalculateDemoReservationBaseAmount(
                hourlyPrice,
                startAt,
                endAt
            );

            var tax = RoundYen(baseAmount * TaxRate);
            var platformFee = RoundYen(baseAmount * PlatformFeeRate);

            return baseAmount + tax + platformFee;
        }

        private static List<ReservationChargeItem> BuildDemoReservationChargeItems(
            Reservation reservation,
            int hourlyPrice)
        {
            var items = new List<ReservationChargeItem>();

            var baseItems = BuildBaseAndMultiplierItems(reservation, hourlyPrice);

            items.AddRange(baseItems);

            var baseAmount = baseItems.Sum(x => x.SliceAmount);

            var tax = RoundYen(baseAmount * TaxRate);
            var platformFee = RoundYen(baseAmount * PlatformFeeRate);

            items.Add(new ReservationChargeItem
            {
                ReservationId = reservation.Id,
                Kind = "tax",
                Description = "消費税 10%",
                SliceStart = null,
                SliceEnd = null,
                UnitRatePerHour = null,
                SliceAmount = tax
            });

            items.Add(new ReservationChargeItem
            {
                ReservationId = reservation.Id,
                Kind = "platform_fee",
                Description = "プラットフォーム手数料 10%",
                SliceStart = null,
                SliceEnd = null,
                UnitRatePerHour = null,
                SliceAmount = platformFee
            });

            return items;
        }

        private static List<ReservationChargeItem> BuildBaseAndMultiplierItems(
            Reservation reservation,
            int hourlyPrice)
        {
            var items = new List<ReservationChargeItem>();

            var normalStart = reservation.StartAt;
            var normalEnd = reservation.EndAt <= reservation.StartAt.Date.AddHours(18)
                ? reservation.EndAt
                : reservation.StartAt.Date.AddHours(18);

            if (normalStart < normalEnd)
            {
                var hours = (decimal)(normalEnd - normalStart).TotalHours;
                var amount = RoundYen(hourlyPrice * hours);

                items.Add(new ReservationChargeItem
                {
                    ReservationId = reservation.Id,
                    Kind = "base",
                    Description = "基本料金",
                    SliceStart = normalStart,
                    SliceEnd = normalEnd,
                    UnitRatePerHour = hourlyPrice,
                    SliceAmount = amount
                });
            }

            var nightStart = reservation.StartAt < reservation.StartAt.Date.AddHours(18)
                ? reservation.StartAt.Date.AddHours(18)
                : reservation.StartAt;

            var nightEnd = reservation.EndAt;

            if (nightStart < nightEnd)
            {
                var unitRate = RoundYen(hourlyPrice * NightMultiplier);
                var hours = (decimal)(nightEnd - nightStart).TotalHours;
                var amount = RoundYen(unitRate * hours);

                items.Add(new ReservationChargeItem
                {
                    ReservationId = reservation.Id,
                    Kind = "multiplier",
                    Description = "夜間料金 18:00以降 1.5倍",
                    SliceStart = nightStart,
                    SliceEnd = nightEnd,
                    UnitRatePerHour = unitRate,
                    SliceAmount = amount
                });
            }

            return items;
        }

        private static int RoundYen(decimal value)
        {
            return (int)Math.Round(value, MidpointRounding.AwayFromZero);
        }

        private static async Task SeedDemoAuditLogsAsync(
    AppDbContext dbContext,
    ILogger logger)
        {
            if (await dbContext.AuditLogs.AnyAsync())
            {
                logger.LogInformation("AuditLog seed skipped. AuditLogs already exist.");
                return;
            }

            var generalUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == "user@example.com");

            var hostUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == "host@example.com");

            var adminUser = await dbContext.Users
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Email == "admin@example.com");

            if (generalUser == null || hostUser == null || adminUser == null)
            {
                logger.LogWarning("AuditLog seed skipped. Required users were not found.");
                return;
            }

            var room = await dbContext.Rooms
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Skip(1)
                .FirstOrDefaultAsync();

            var reservations = await dbContext.Reservations
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .ToListAsync();

            var approveReservation = reservations.ElementAtOrDefault(122)
                ?? reservations.LastOrDefault();

            var cancelReservation = reservations.ElementAtOrDefault(120)
                ?? reservations.LastOrDefault();

            var roomId = room?.Id ?? 2;
            var roomName = room?.Name ?? "中央区 銀座フォトスタジオ";

            var approveReservationId = approveReservation?.Id ?? 123;
            var cancelReservationId = cancelReservation?.Id ?? 121;

            var logs = new List<AuditLog>
    {
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 20, 0),
            ActorId = adminUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = adminUser.Id,
            Note = $"ユーザー「{adminUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 20, 0),
            ActorId = hostUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = hostUser.Id,
            Note = $"ユーザー「{hostUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 21, 0),
            ActorId = adminUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = adminUser.Id,
            Note = $"ユーザー「{adminUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 25, 0),
            ActorId = adminUser.Id,
            Action = "SETTING_UPDATE",
            Entity = "AppSetting",
            EntityId = null,
            Note = "管理設定を更新しました。税率: 50% / 手数料: 15%"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 25, 30),
            ActorId = adminUser.Id,
            Action = "SETTING_UPDATE",
            Entity = "AppSetting",
            EntityId = null,
            Note = "管理設定を更新しました。税率: 10% / 手数料: 15%"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 33, 0),
            ActorId = adminUser.Id,
            Action = "UPDATE",
            Entity = "Room",
            EntityId = roomId,
            Note = $"スタジオ「{roomName}」の基本情報を更新しました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 34, 0),
            ActorId = adminUser.Id,
            Action = "UPDATE",
            Entity = "Room",
            EntityId = roomId,
            Note = $"スタジオ「{roomName}」の基本情報を更新しました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 34, 30),
            ActorId = hostUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = hostUser.Id,
            Note = $"ユーザー「{hostUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 37, 0),
            ActorId = hostUser.Id,
            Action = "APPROVE",
            Entity = "Reservation",
            EntityId = approveReservationId,
            Note = $"ホストユーザーが予約ID「{approveReservationId}」を承認しました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 37, 30),
            ActorId = hostUser.Id,
            Action = "CANCEL",
            Entity = "Reservation",
            EntityId = cancelReservationId,
            Note = $"ホストユーザーが予約ID「{cancelReservationId}」をキャンセルしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 0, 37, 50),
            ActorId = adminUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = adminUser.Id,
            Note = $"ユーザー「{adminUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 1, 42, 0),
            ActorId = generalUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = generalUser.Id,
            Note = $"ユーザー「{generalUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 2, 4, 0),
            ActorId = generalUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = generalUser.Id,
            Note = $"ユーザー「{generalUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 21, 11, 0),
            ActorId = generalUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = generalUser.Id,
            Note = $"ユーザー「{generalUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 18, 0),
            ActorId = hostUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = hostUser.Id,
            Note = $"ユーザー「{hostUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 19, 0),
            ActorId = adminUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = adminUser.Id,
            Note = $"ユーザー「{adminUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 20, 0),
            ActorId = generalUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = generalUser.Id,
            Note = $"ユーザー「{generalUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 22, 0),
            ActorId = adminUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = adminUser.Id,
            Note = $"ユーザー「{adminUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 29, 0),
            ActorId = generalUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = generalUser.Id,
            Note = $"ユーザー「{generalUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 30, 0),
            ActorId = adminUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = adminUser.Id,
            Note = $"ユーザー「{adminUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 37, 0),
            ActorId = generalUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = generalUser.Id,
            Note = $"ユーザー「{generalUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 45, 0),
            ActorId = adminUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = adminUser.Id,
            Note = $"ユーザー「{adminUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 22, 47, 0),
            ActorId = hostUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = hostUser.Id,
            Note = $"ユーザー「{hostUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 23, 3, 0),
            ActorId = generalUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = generalUser.Id,
            Note = $"ユーザー「{generalUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 23, 9, 0),
            ActorId = hostUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = hostUser.Id,
            Note = $"ユーザー「{hostUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 23, 23, 0),
            ActorId = generalUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = generalUser.Id,
            Note = $"ユーザー「{generalUser.Name}」がログインしました。"
        },
        new()
        {
            Ts = new DateTime(2026, 4, 30, 23, 34, 0),
            ActorId = adminUser.Id,
            Action = "LOGIN",
            Entity = "User",
            EntityId = adminUser.Id,
            Note = $"ユーザー「{adminUser.Name}」がログインしました。"
        }
    };

            dbContext.AuditLogs.AddRange(logs);
            await dbContext.SaveChangesAsync();

            logger.LogInformation("AuditLog seed inserted. Count={Count}", logs.Count);
        }
    }
}