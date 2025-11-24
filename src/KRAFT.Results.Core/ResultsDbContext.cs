using KRAFT.Results.Core.Ads;
using KRAFT.Results.Core.AdSlots;
using KRAFT.Results.Core.AgeCategories;
using KRAFT.Results.Core.AthleteAliases;
using KRAFT.Results.Core.Athletes;
using KRAFT.Results.Core.Attempts;
using KRAFT.Results.Core.Bans;
using KRAFT.Results.Core.Cases;
using KRAFT.Results.Core.Countries;
using KRAFT.Results.Core.Eras;
using KRAFT.Results.Core.EraWeightCategories;
using KRAFT.Results.Core.Meets;
using KRAFT.Results.Core.MeetTypes;
using KRAFT.Results.Core.PageGroups;
using KRAFT.Results.Core.Pages;
using KRAFT.Results.Core.Participations;
using KRAFT.Results.Core.Photos;
using KRAFT.Results.Core.Records;
using KRAFT.Results.Core.Roles;
using KRAFT.Results.Core.Teams;
using KRAFT.Results.Core.UserRoles;
using KRAFT.Results.Core.Users;
using KRAFT.Results.Core.WeightCategories;
using KRAFT.Results.Core.Wilks;

using Microsoft.EntityFrameworkCore;

namespace KRAFT.Results.Core;

internal sealed class ResultsDbContext : DbContext
{
    public ResultsDbContext(DbContextOptions<ResultsDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ad> Ads { get; set; }

    public DbSet<AdEvent> AdEvents { get; set; }

    public DbSet<AdSlot> AdSlots { get; set; }

    public DbSet<AgeCategory> AgeCategories { get; set; }

    public DbSet<Athlete> Athletes { get; set; }

    public DbSet<AthleteAlias> AthleteAliases { get; set; }

    public DbSet<Attempt> Attempts { get; set; }

    public DbSet<Ban> Bans { get; set; }

    public DbSet<Case> Cases { get; set; }

    public DbSet<Country> Countries { get; set; }

    public DbSet<Era> Eras { get; set; }

    public DbSet<EraWeightCategory> EraWeightCategories { get; set; }

    public DbSet<Meet> Meets { get; set; }

    public DbSet<MeetType> MeetTypes { get; set; }

    public DbSet<Page> Pages { get; set; }

    public DbSet<PageGroup> PageGroups { get; set; }

    public DbSet<Participation> Participations { get; set; }

    public DbSet<Photo> Photos { get; set; }

    public DbSet<Record> Records { get; set; }

    public DbSet<Role> Roles { get; set; }

    public DbSet<Team> Teams { get; set; }

    public DbSet<User> Users { get; set; }

    public DbSet<UserRole> UserRoles { get; set; }

    public DbSet<WeightCategory> WeightCategories { get; set; }

    public DbSet<Wilk> Wilks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResultsDbContext).Assembly);
    }
}