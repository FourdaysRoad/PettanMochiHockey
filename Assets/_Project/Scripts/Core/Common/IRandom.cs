namespace MochiHockey.Core
{
    /// <summary>乱数。テストで決まった値に差し替えられるように注入する。</summary>
    public interface IRandom
    {
        /// <summary>0 以上 1 未満の乱数。</summary>
        float NextFloat();
    }
}
