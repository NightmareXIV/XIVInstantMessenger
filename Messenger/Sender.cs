namespace Messenger
{
    internal struct Sender : IEquatable<Sender>
    {
        internal string Name;
        internal uint HomeWorld;

        internal Sender(string Name, uint HomeWorld)
        {
            this.Name = Name;
            this.HomeWorld = HomeWorld;
        }

        public override bool Equals(object obj)
        {
            return obj is Sender sender && Equals(sender);
        }

        public bool Equals(Sender other)
        {
            return Name == other.Name &&
                   HomeWorld == other.HomeWorld;
        }

        public override int GetHashCode()
        {
            return HashCode.Combine(Name, HomeWorld);
        }

        public override string ToString()
        {
            return this.GetPlayerName();
        }

        public static bool operator ==(Sender left, Sender right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(Sender left, Sender right)
        {
            return !(left == right);
        }
    }
}
