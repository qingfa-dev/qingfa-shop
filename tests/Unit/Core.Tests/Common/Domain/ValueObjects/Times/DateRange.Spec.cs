using Core.Common.Domain.ValueObjects.Times;

namespace Core.Tests.Domain.ValueObjects.Times;

public class DateRangeSpec
{
    [Fact]
    public void Create_OrderedDates_ShouldSucceed()
    {
        var result = DateRange.Create(
            new DateOnly(2026, 1, 1),
            new DateOnly(2026, 1, 2));

        result.IsSuccess.Should().BeTrue();
    }

    [Fact]
    public void Create_EndBeforeStart_ShouldFailWithInvalidRange()
    {
        var result = DateRange.Create(
            new DateOnly(2026, 1, 2),
            new DateOnly(2026, 1, 1));

        result.IsFailure.Should().BeTrue();
        result.Errors![0].Code.Should().Be("DateRange.Range.Invalid");
    }

    [Fact]
    public void Create_SingleDayRange_ShouldSucceedWithDurationOne()
    {
        // Arrange:
        var date = new DateOnly(2026, 1, 1);

        // Act:
        var result = DateRange.Create(date, date);

        // Assert:
        result.IsSuccess.Should().BeTrue();
        result.Value.Start.Should().Be(date);
        result.Value.End.Should().Be(date);
        result.Value.Duration().Should().Be(1);
    }

    [Fact]
    public void RecordEquality_SameDates_ShouldBeEqual()
    {
        var range = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)).Value;
        var equivalent = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 2)).Value;
        var changed = DateRange.Create(new DateOnly(2026, 1, 1), new DateOnly(2026, 1, 3)).Value;

        range.Should().Be(equivalent);
        range.Should().NotBe(changed);
    }
}