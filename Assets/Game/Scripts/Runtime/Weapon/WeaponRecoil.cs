using UnityEngine;

/// <summary>
/// 武器后坐：把后坐力写进视角，使准星上抬/偏移；停火后只恢复尚未被压枪抵消的部分。
/// </summary>
public sealed class WeaponRecoil
{
    private MoveComponent _move;
    private Vector2 _recoverable;
    private int _burstIndex;
    private float _lastKickTime = -999f;

    public bool HasPendingRecovery => _recoverable.sqrMagnitude > 0.000001f;

    public void Bind(MoveComponent move)
    {
        _move = move;
    }

    public void ResetBurst()
    {
        _burstIndex = 0;
    }

    public void Clear()
    {
        _burstIndex = 0;
        _recoverable = Vector2.zero;
    }

    /// <summary>
    /// 鼠标转动若与后坐方向相反，视为压枪，减少后续自动回正量。
    /// </summary>
    public void AbsorbLook(float yawDelta, float pitchDelta)
    {
        _recoverable.x = AbsorbAxis(_recoverable.x, yawDelta);
        _recoverable.y = AbsorbAxis(_recoverable.y, pitchDelta);
    }

    public bool Tick(WeaponDefinition definition, float deltaTime, float now, bool suppressRecovery)
    {
        if (definition == null)
            return false;

        if (now - _lastKickTime >= definition.RecoilResetTime)
            _burstIndex = 0;

        if (suppressRecovery || !HasPendingRecovery)
            return false;

        if (now - _lastKickTime < definition.RecoilRecoveryDelay)
            return false;

        var t = 1f - Mathf.Exp(-definition.RecoilRecoverySpeed * deltaTime);
        var step = _recoverable * t;
        ApplyView(-step.x, -step.y);
        _recoverable -= step;
        if (_recoverable.sqrMagnitude < 0.000001f)
            _recoverable = Vector2.zero;

        return true;
    }

    public bool Kick(WeaponDefinition definition, bool aiming, float now)
    {
        if (definition == null || !definition.RecoilEnabled || _move == null)
            return false;

        var progress = definition.MagazineSize > 1
            ? Mathf.Clamp01(_burstIndex / (float)(definition.MagazineSize - 1))
            : 0f;
        var ads = aiming ? definition.AdsRecoilScale : 1f;
        var vertical = definition.VerticalRecoil *
                       Mathf.Max(0f, Evaluate(definition.VerticalRecoilPattern, progress)) * ads;
        var horizontal = definition.HorizontalRecoil *
                         Evaluate(definition.HorizontalRecoilPattern, progress) * ads;

        var randomness = definition.RecoilRandomness;
        if (randomness > 0f)
        {
            vertical *= 1f + Random.Range(-randomness, randomness);
            horizontal *= 1f + Random.Range(-randomness, randomness);
        }

        horizontal += definition.HorizontalBias * definition.HorizontalRecoil * ads;
        vertical = Mathf.Max(0f, vertical);
        vertical = Mathf.Min(vertical, Mathf.Max(0f, definition.MaxVerticalRecoil + _recoverable.y));

        var yawTarget = Mathf.Clamp(
            _recoverable.x + horizontal,
            -definition.MaxHorizontalRecoil,
            definition.MaxHorizontalRecoil);
        horizontal = yawTarget - _recoverable.x;

        if (vertical <= 0.00001f && Mathf.Abs(horizontal) <= 0.00001f)
            return false;

        var yawBefore = _move.ViewYaw;
        var pitchBefore = _move.ViewPitch;
        ApplyView(horizontal, -vertical);

        var applied = new Vector2(_move.ViewYaw - yawBefore, _move.ViewPitch - pitchBefore);
        _recoverable.x = Mathf.Clamp(
            _recoverable.x + applied.x,
            -definition.MaxHorizontalRecoil,
            definition.MaxHorizontalRecoil);
        _recoverable.y = Mathf.Clamp(
            _recoverable.y + applied.y,
            -definition.MaxVerticalRecoil,
            0f);

        _burstIndex++;
        _lastKickTime = now;
        return applied.sqrMagnitude > 0.000001f;
    }

    private void ApplyView(float yawDelta, float pitchDelta)
    {
        if (_move == null)
            return;

        _move.AddViewRotation(yawDelta, pitchDelta);
    }

    private static float Evaluate(AnimationCurve curve, float t)
    {
        if (curve == null || curve.length == 0)
            return 1f;

        return curve.Evaluate(t);
    }

    private static float AbsorbAxis(float remaining, float lookDelta)
    {
        if (Mathf.Abs(remaining) < 0.00001f || Mathf.Abs(lookDelta) < 0.00001f)
            return remaining;

        if (Mathf.Sign(lookDelta) == Mathf.Sign(remaining))
            return remaining;

        var combined = remaining + lookDelta;
        return Mathf.Sign(combined) == Mathf.Sign(remaining) ? combined : 0f;
    }
}
