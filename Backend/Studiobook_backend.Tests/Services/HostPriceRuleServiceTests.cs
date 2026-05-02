using Microsoft.EntityFrameworkCore;
using Studiobook_backend.Data;
using Studiobook_backend.Dtos.Host;
using Studiobook_backend.Entities;
using Studiobook_backend.Services;
using Studiobook_backend.Tests.Helpers;

namespace Studiobook_backend.Tests.Services;

public class HostPriceRuleServiceTests
{
    [Fact]
    public async Task GetAsync_ReturnsRules_WhenRoomBelongsToUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 10,
            Name = "テストスタジオ"
        });

        context.PriceRules.AddRange(
            new PriceRule
            {
                Id = 1,
                RoomId = 1,
                RuleType = "multiplier",
                Weekday = 1,
                StartHour = new TimeOnly(19, 0),
                EndHour = new TimeOnly(22, 0),
                Multiplier = 1.5m,
                FlatFee = null,
                Note = "平日夜間",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            },
            new PriceRule
            {
                Id = 2,
                RoomId = 1,
                RuleType = "flat_fee",
                Weekday = null,
                StartHour = null,
                EndHour = null,
                Multiplier = null,
                FlatFee = 1000,
                Note = "共通固定費",
                CreatedAtUtc = DateTime.UtcNow,
                UpdatedAtUtc = DateTime.UtcNow
            }
        );

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);

        // Act
        var result = await service.GetAsync(roomId: 1, userId: 10);

        // Assert
        Assert.Equal(1, result.RoomId);
        Assert.Equal("テストスタジオ", result.RoomName);
        Assert.Equal(2, result.Rules.Count);

        // Weekday == null が先頭に来る想定
        Assert.Equal("flat_fee", result.Rules[0].RuleType);
        Assert.Null(result.Rules[0].Weekday);
        Assert.Equal(1000, result.Rules[0].FlatFee);

        Assert.Equal("multiplier", result.Rules[1].RuleType);
        Assert.Equal(1, result.Rules[1].Weekday);
        Assert.Equal("19:00", result.Rules[1].StartHour);
        Assert.Equal("22:00", result.Rules[1].EndHour);
        Assert.Equal(1.5m, result.Rules[1].Multiplier);
    }

    [Fact]
    public async Task GetAsync_ThrowsKeyNotFoundException_WhenRoomDoesNotBelongToUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 99,
            Name = "他人のスタジオ"
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.GetAsync(roomId: 1, userId: 10));
    }

    [Fact]
    public async Task AddAsync_AddsMultiplierRule_WhenRequestIsValid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 10,
            Name = "テストスタジオ"
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 1.5m,
            FlatFee = null,
            Note = "夜間料金"
        };

        // Act
        await service.AddAsync(roomId: 1, userId: 10, request);

        // Assert
        var saved = Assert.Single(await context.PriceRules.ToListAsync());

        Assert.Equal(1, saved.RoomId);
        Assert.Equal("multiplier", saved.RuleType);
        Assert.Equal(1, saved.Weekday);
        Assert.Equal(new TimeOnly(19, 0), saved.StartHour);
        Assert.Equal(new TimeOnly(22, 0), saved.EndHour);
        Assert.Equal(1.5m, saved.Multiplier);
        Assert.Null(saved.FlatFee);
        Assert.Equal("夜間料金", saved.Note);
    }

    [Fact]
    public async Task AddAsync_AddsFlatFeeRule_WhenRequestIsValid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 10,
            Name = "テストスタジオ"
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "flat_fee",
            Weekday = 6,
            StartHour = null,
            EndHour = null,
            Multiplier = null,
            FlatFee = 1000,
            Note = "土曜固定費"
        };

        // Act
        await service.AddAsync(roomId: 1, userId: 10, request);

        // Assert
        var saved = Assert.Single(await context.PriceRules.ToListAsync());

        Assert.Equal(1, saved.RoomId);
        Assert.Equal("flat_fee", saved.RuleType);
        Assert.Equal(6, saved.Weekday);
        Assert.Null(saved.StartHour);
        Assert.Null(saved.EndHour);
        Assert.Null(saved.Multiplier);
        Assert.Equal(1000, saved.FlatFee);
        Assert.Equal("土曜固定費", saved.Note);
    }

    [Fact]
    public async Task AddAsync_ThrowsKeyNotFoundException_WhenRoomDoesNotBelongToUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        context.Rooms.Add(new Room
        {
            Id = 1,
            UserId = 99,
            Name = "他人のスタジオ"
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 1.5m,
            FlatFee = null,
            Note = "夜間料金"
        };

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Empty(context.PriceRules);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenRuleTypeIsEmpty()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 1.5m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("タイプを選択してください。", ex.Message);
        Assert.Empty(context.PriceRules);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenWeekdayIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 7,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 1.5m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("曜日の指定が不正です。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenFlatFeeIsNull()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "flat_fee",
            Weekday = 1,
            FlatFee = null
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("固定費を入力してください。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenFlatFeeIsNegative()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "flat_fee",
            Weekday = 1,
            FlatFee = -1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("固定費は0以上で入力してください。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenFlatFeeHasTimeOrMultiplier()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "flat_fee",
            Weekday = 1,
            StartHour = "09:00",
            EndHour = null,
            Multiplier = null,
            FlatFee = 1000
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("固定費の場合、開始/終了時刻と倍率は入力できません。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenDuplicateFlatFeeExistsForSameWeekday()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedRoomAsync(context, roomId: 1, userId: 10);

        context.PriceRules.Add(new PriceRule
        {
            Id = 1,
            RoomId = 1,
            RuleType = "flat_fee",
            Weekday = 6,
            FlatFee = 1000,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "flat_fee",
            Weekday = 6,
            FlatFee = 2000
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("同一曜日の固定費はすでに登録されています。", ex.Message);

        Assert.Equal(1, await context.PriceRules.CountAsync());
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenMultiplierIsNull()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = null
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("倍率を入力してください。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenMultiplierIsZero()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 0
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("倍率は0より大きい値を入力してください。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenMultiplierHasFlatFee()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "19:00",
            EndHour = "22:00",
            Multiplier = 1.5m,
            FlatFee = 1000
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("倍率の場合、固定費は入力できません。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenStartHourIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "invalid",
            EndHour = "22:00",
            Multiplier = 1.5m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("時刻の形式が不正です。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenTimeIsNotQuarter()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "19:10",
            EndHour = "22:00",
            Multiplier = 1.5m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("開始・終了は15分刻みで指定してください。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenEndHourIsBeforeStartHour()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "multiplier",
            Weekday = 1,
            StartHour = "22:00",
            EndHour = "19:00",
            Multiplier = 1.5m
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("終了時刻は開始時刻より後にしてください。", ex.Message);
    }

    [Fact]
    public async Task AddAsync_ThrowsArgumentException_WhenRuleTypeIsInvalid()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();
        await SeedRoomAsync(context, roomId: 1, userId: 10);

        var service = new HostPriceRuleService(context);

        var request = new CreatePriceRuleRequestDto
        {
            RuleType = "unknown",
            Weekday = 1
        };

        // Act & Assert
        var ex = await Assert.ThrowsAsync<ArgumentException>(() =>
            service.AddAsync(roomId: 1, userId: 10, request));

        Assert.Equal("不正なタイプです。", ex.Message);
    }

    [Fact]
    public async Task DeleteAsync_RemovesRule_WhenRuleBelongsToUserRoom()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedRoomAsync(context, roomId: 1, userId: 10);

        context.PriceRules.Add(new PriceRule
        {
            Id = 1,
            RoomId = 1,
            RuleType = "flat_fee",
            Weekday = 6,
            FlatFee = 1000,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);

        // Act
        await service.DeleteAsync(roomId: 1, ruleId: 1, userId: 10);

        // Assert
        Assert.Empty(context.PriceRules);
    }

    [Fact]
    public async Task DeleteAsync_ThrowsKeyNotFoundException_WhenRuleDoesNotBelongToUser()
    {
        // Arrange
        await using var context = TestDbContextFactory.Create();

        await SeedRoomAsync(context, roomId: 1, userId: 99);

        context.PriceRules.Add(new PriceRule
        {
            Id = 1,
            RoomId = 1,
            RuleType = "flat_fee",
            Weekday = 6,
            FlatFee = 1000,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        });

        await context.SaveChangesAsync();

        var service = new HostPriceRuleService(context);

        // Act & Assert
        await Assert.ThrowsAsync<KeyNotFoundException>(() =>
            service.DeleteAsync(roomId: 1, ruleId: 1, userId: 10));

        Assert.Single(context.PriceRules);
    }

    private static async Task SeedRoomAsync(
        AppDbContext context,
        int roomId,
        int userId)
    {
        context.Rooms.Add(new Room
        {
            Id = roomId,
            UserId = userId,
            Name = "テストスタジオ"
        });

        await context.SaveChangesAsync();
    }
}