using KRAFT.Results.WebApi.Abstractions;

namespace KRAFT.Results.WebApi.ValueObjects;

/// <summary>
/// https://www.powerlifting.sport/fileadmin/ipf/data/ipf-formula/IPF_GL_Coefficients-2020.pdf.
/// </summary>
internal sealed class IpfPoints : ValueObject<decimal>
{
    private const string Male = "m";
    private const string Female = "f";
    private const string Powerlifting = "Powerlifting";
    private const string Benchpress = "Benchpress";

    private static readonly IpfPoints None = new(0);

    private IpfPoints(decimal value)
        : base(value)
    {
    }

    public static IpfPoints Create(bool isClassic, Gender gender, string type, decimal bodyWeight, decimal weight)
    {
        if (gender.Value is Male && bodyWeight < 40)
        {
            return None;
        }

        if (gender.Value is Female && bodyWeight < 35)
        {
            return None;
        }

        decimal a = (isClassic, gender.Value, type) switch
        {
            (false, Male, Powerlifting) => 1236.25115m,
            (true, Male, Powerlifting) => 1199.72839m,
            (false, Male, Benchpress) => 381.22073m,
            (true, Male, Benchpress) => 320.98041m,
            (false, Female, Powerlifting) => 758.63878m,
            (true, Female, Powerlifting) => 610.32796m,
            (false, Female, Benchpress) => 221.82209m,
            (true, Female, Benchpress) => 142.40398m,
            _ => 0,
        };

        decimal b = (isClassic, gender.Value, type) switch
        {
            (false, Male, Powerlifting) => 1449.21864m,
            (true, Male, Powerlifting) => 1025.18162m,
            (false, Male, Benchpress) => 733.79378m,
            (true, Male, Benchpress) => 281.40258m,
            (false, Female, Powerlifting) => 949.31382m,
            (true, Female, Powerlifting) => 1045.59282m,
            (false, Female, Benchpress) => 357.00377m,
            (true, Female, Benchpress) => 442.52671m,
            _ => 0,
        };

        decimal c = (isClassic, gender.Value, type) switch
        {
            (false, Male, Powerlifting) => 0.01644m,
            (true, Male, Powerlifting) => 0.00921m,
            (false, Male, Benchpress) => 0.02398m,
            (true, Male, Benchpress) => 0.01008m,
            (false, Female, Powerlifting) => 0.02435m,
            (true, Female, Powerlifting) => 0.03048m,
            (false, Female, Benchpress) => 0.02937m,
            (true, Female, Benchpress) => 0.04724m,
            _ => 0,
        };

        if (a == 0 || b == 0 || c == 0)
        {
            return None;
        }

        decimal power = -c * bodyWeight;
        decimal denominator = a - (b * (decimal)Math.Exp((double)power));
        decimal coefficient = 100 / denominator;

        return new IpfPoints(coefficient * weight);
    }
}