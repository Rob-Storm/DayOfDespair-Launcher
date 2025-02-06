namespace DoDLauncher.Util
{
    public struct Version
    {
        public int Major {  get; set; }
        public int Minor { get; set; }
        public int Patch { get; set; }

        public Version(int major, int minor, int patch)
        {
            Major = major;
            Minor = minor;
            Patch = patch;
        }

        public static Version FromString(string input)
        {
            string[] versionInput = input.Split('.');

            int versionMajor = int.Parse(versionInput[0]);
            int versionMinor = int.Parse(versionInput[1]);
            int versionPatch = int.Parse(versionInput[2]);

            return new Version(versionMajor, versionMinor, versionPatch);
        }

        public override string ToString()
        {
            return $"{Major}.{Minor}.{Patch}";
        }

        #region Inequality Operators

        public static bool operator >(Version v1, Version v2)
        {
            if(v1.Major != v2.Major) return v1.Major > v2.Major;
            if(v1.Minor != v2.Minor) return v1.Minor > v2.Minor;
            return v1.Patch > v2.Patch;
        }

        public static bool operator <(Version v1, Version v2)
        {
            if (v1.Major != v2.Major) return v1.Major < v2.Major;
            if (v1.Minor != v2.Minor) return v1.Minor < v2.Minor;
            return v1.Patch < v2.Patch;
        }

        public static bool operator >=(Version v1, Version v2)
        {
            return v1 > v2 || v1 == v2;
        }
        public static bool operator <=(Version v1, Version v2)
        {
            return v1 < v2 || v1 == v2;
        }

        public static bool operator ==(Version v1, Version v2)
        {
            return v1.Major == v2.Major && v1.Minor == v2.Minor && v1.Patch == v2.Patch;
        }

        public static bool operator !=(Version v1, Version v2)
        {
            return !(v1 == v2);
        }

        #endregion
    }
}
