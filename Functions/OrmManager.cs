using ModKit.ORM;
using SQLite;

namespace MODRP_JobTaxi.Functions
{
    internal class OrmManager
    {

        public class JobTaxi_JobTaxiManager : ModEntity<JobTaxi_JobTaxiManager>
        {
            [AutoIncrement][PrimaryKey] public int Id { get; set; }

            public float PositionX { get; set; }
            public float PositionY { get; set; }
            public float PositionZ { get; set; }
        }

    }
}
