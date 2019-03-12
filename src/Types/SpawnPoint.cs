namespace WildernessCallouts.Types
{
    using Rage;

    internal class SpawnPoint
    {
        public Vector3 Position;
        public float Heading;

        public SpawnPoint(Vector3 position, float heading)
        {
            Position = position;
            Heading = heading;
        }


        public static SpawnPoint Zero
        {
            get { return new SpawnPoint(Vector3.Zero, 0f); }
        }


        public static bool operator ==(SpawnPoint left, SpawnPoint right)
        {
            return left.Position == right.Position && left.Heading == right.Heading;
        }

        public static bool operator !=(SpawnPoint left, SpawnPoint right)
        {
            return left.Position != right.Position || left.Heading != right.Heading;
        }

        public static SpawnPoint operator +(SpawnPoint left, SpawnPoint right)
        {
            return new SpawnPoint(left.Position + right.Position, left.Heading + right.Heading);
        }

        public static SpawnPoint operator -(SpawnPoint left, SpawnPoint right)
        {
            return new SpawnPoint(left.Position - right.Position, left.Heading - right.Heading);
        }

        public override bool Equals(object obj)
        {
            if (obj.GetType() != typeof(SpawnPoint))
                throw new System.InvalidCastException();

            return Equals((SpawnPoint)obj);
        }

        public bool Equals(SpawnPoint spawnPoint)
        {
            return this.Position == spawnPoint.Position && this.Heading == spawnPoint.Heading;
        }

        public override int GetHashCode()
        {
            int hash = 11;
            hash = (hash * 7) + this.Position.GetHashCode();
            hash = (hash * 7) + this.Heading.GetHashCode();
            return hash;
        }
    }
}
