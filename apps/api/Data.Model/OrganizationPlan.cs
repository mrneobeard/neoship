

namespace NeoShip.Data.Model;

public class OrganizationPlan
{
    public OrganizationPlan()
    {
    }

    public OrganizationPlan(ushort id, string name)
    {
        this.Id = id;
        this.Name = name;
    }

    public ushort Id { get; set; }

    public string Name { get; set; } = string.Empty;


    public static OrganizationPlan Free => new(1, "Free");

    public static OrganizationPlan Hobbyist => new(2, "Hobbyist");

    public static OrganizationPlan Team => new(3, "Team");

    public static OrganizationPlan Enterprise => new(4, "Enterprise");

    public static IEnumerable<OrganizationPlan> All => [Free, Hobbyist, Team, Enterprise];

}