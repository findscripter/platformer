/// <summary>
/// 可受伤目标。玩家与敌人都实现此接口，攻击方只依赖抽象、不关心具体类型。
/// </summary>
public interface IDamageable
{
    int CurrentHealth { get; }
    int MaxHealth { get; }
    bool IsAlive { get; }

    /// <summary>当前是否可被伤害（无敌帧期间为 false）。</summary>
    bool IsDamageable { get; }

    /// <summary>
    /// 施加伤害。无敌帧期间调用会被忽略。
    /// </summary>
    /// <param name="amount">伤害量。</param>
    /// <returns>是否真正造成了伤害。</returns>
    bool TakeDamage(int amount);

    /// <summary>
    /// 直接致死，绕过血量与无敌帧。用于坠落深渊等环境即死。
    /// </summary>
    void Kill();
}
