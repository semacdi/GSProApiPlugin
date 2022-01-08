namespace GSProApiPlugin
{
    public class GSShot
    {
        public string DeviceID { get; set; }
        public string Units { get; set; }
        public int ShotNumber { get; set; }
        public int APIVersion { get; set; }
        public bool? Cheating { get; set; }
        public GSBallData BallData { get; set; }
        public GSClubData ClubData { get; set; }
        public GSShotOptions ShotDataOptions { get; set; }
    }
}
