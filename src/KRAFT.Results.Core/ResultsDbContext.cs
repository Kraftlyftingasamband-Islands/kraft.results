using KRAFT.Results.Core.AdEvents;
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

internal class ResultsDbContext : DbContext
{
    public ResultsDbContext(DbContextOptions<ResultsDbContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Ad> Ads { get; set; }

    public virtual DbSet<AdEvent> AdEvents { get; set; }

    public virtual DbSet<AdSlot> AdSlots { get; set; }

    public virtual DbSet<AgeCategory> AgeCategories { get; set; }

    public virtual DbSet<Athlete> Athletes { get; set; }

    public virtual DbSet<AthleteAlias> AthleteAliases { get; set; }

    public virtual DbSet<Attempt> Attempts { get; set; }

    public virtual DbSet<Ban> Bans { get; set; }

    public virtual DbSet<Case> Cases { get; set; }

    public virtual DbSet<Country> Countries { get; set; }

    public virtual DbSet<Era> Eras { get; set; }

    public virtual DbSet<EraWeightCategory> EraWeightCategories { get; set; }

    public virtual DbSet<Meet> Meets { get; set; }

    public virtual DbSet<MeetType> MeetTypes { get; set; }

    public virtual DbSet<Page> Pages { get; set; }

    public virtual DbSet<PageGroup> PageGroups { get; set; }

    public virtual DbSet<Participation> Participations { get; set; }

    public virtual DbSet<Photo> Photos { get; set; }

    public virtual DbSet<Record> Records { get; set; }

    public virtual DbSet<Role> Roles { get; set; }

    public virtual DbSet<Team> Teams { get; set; }

    public virtual DbSet<User> Users { get; set; }

    public virtual DbSet<UserRole> UserRoles { get; set; }

    public virtual DbSet<WeightCategory> WeightCategories { get; set; }

    public virtual DbSet<Wilk> Wilks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(ResultsDbContext).Assembly);
    }
}