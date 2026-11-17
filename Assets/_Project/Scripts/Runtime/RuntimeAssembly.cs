namespace MochiHockey.Runtime
{
    /// <summary>
    /// MochiHockey.Runtime アセンブリの目印。テストからアセンブリを特定するために使う。
    /// </summary>
    public static class RuntimeAssembly
    {
        /// <summary>Runtime が Core を参照していることをテストで確かめるための型。</summary>
        public static System.Type CoreMarker => typeof(MochiHockey.Core.CoreAssembly);
    }
}
