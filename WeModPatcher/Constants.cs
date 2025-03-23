using System;
using System.Reflection;

namespace WeModPatcher
{
    public static class Constants
    {
        public const string RepoName = "Wemod-Patcher";
        public const string Owner = "k1tbyte";
        public static readonly string RepositoryUrl = $"https://github.com/{Owner}/{RepoName}";
        public static readonly Version Version;

        static  Constants()
        {
            Version = Assembly.GetExecutingAssembly().GetName().Version;
        }
    }
}