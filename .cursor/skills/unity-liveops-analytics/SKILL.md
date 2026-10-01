---
name: unity-liveops-analytics
description: Use when implementing telemetry, analytics, remote config, feature flags, or A/B testing in a Unity game — event taxonomy, consent, privacy compliance, crash reporting, and rollout strategy.
---

# unity-liveops-analytics

Live-ops is what happens **after** ship. Design events, config, and flags before launch — retrofitting is painful.

## Privacy & compliance — read first

- **GDPR (EU)** — explicit consent for tracking, right to deletion, data export.
- **CCPA (California)** — opt-out of sale, disclosure.
- **COPPA (US, kids)** — no tracking under 13 without parental consent.
- **ATT (iOS 14.5+)** — explicit prompt for cross-app tracking.
- **Play Store Data Safety** — declare what you collect.
- **App Store Privacy Manifest** — required since 2024.

**Never log PII** — no emails, names, phone numbers, precise location. Player IDs (hashed) only.

## Consent flow

1. **First launch** — show consent dialog (GDPR regions).
2. **Store consent** locally + remotely.
3. **Respect consent** across all analytics SDKs.
4. **Provide a "withdraw consent"** path in settings.
5. **Re-consent** if terms change.

## Event taxonomy

Structure events as **category.action**:

```
game.session_start
game.level_start        { level_id, difficulty }
game.level_complete     { level_id, time, deaths, stars }
game.level_fail         { level_id, reason, progress_pct }
game.purchase_initiated { item_id, currency, price_usd }
game.purchase_completed { item_id, currency, price_usd, txn_id }
ui.screen_viewed        { screen_name }
economy.currency_spent  { currency, amount, sink }
```

**Rules:**
- **Past tense for completed** (`purchase_completed`), **present for ongoing** (`level_start`).
- **Lowercase snake_case.**
- **Parameters typed and documented.**
- **Version events** — add fields, don't rename.
- **No PII in params.**

## SDK choices

| SDK | Best for | Notes |
|---|---|---|
| **Unity Analytics** | Unity-native, simple | GDPR-friendly, UGS-integrated |
| **GameAnalytics** | Indie, free | Good dashboards, generous free tier |
| **Firebase Analytics** | Mobile, Google ecosystem | Free, powerful, requires Google |
| **Custom backend** | Full control, custom metrics | More work, best for scale |
| **Mixpanel / Amplitude** | Product analytics | Expensive at scale |

**Don't run three SDKs.** Pick one primary, add a crash reporter.

## Remote config

```csharp
public interface IRemoteConfig
{
    T Get<T>(string key, T defaultValue);
    Task FetchAsync(CancellationToken ct);
}

// Usage
float dropRate = RemoteConfig.Get("drop_rate_epic", 0.02f);
```

**Rules:**
- **Always provide a default** — network can fail.
- **Cache last-known-good** locally.
- **Version config schema.**
- **Validate on read** (range check, type check).
- **Never crash on missing key** — return default.

## Feature flags

```csharp
if (FeatureFlags.IsEnabled("new_matchmaking"))
{
    // new code path
}
else
{
    // old code path
}
```

**Rules:**
- **Flags are temporary.** Every flag has a removal date.
- **Default = off** for new features.
- **Kill switch** for risky features.
- **Target by cohort** (region, device, version, user ID hash).
- **Never nest flags** — combinatorial hell.

## A/B testing

**Before you start:**
- **Hypothesis**: "Showing X first increases conversion by Y%."
- **Primary metric** — one number that decides success.
- **Guardrail metrics** — what must not regress (crash rate, D1 retention).
- **Sample size** — compute it; don't fish.
- **Duration** — minimum 1–2 weeks to cover weekly cycles.

**During:**
- **Randomize** at user ID level, not session.
- **Sticky assignment** — same user, same variant, forever.
- **No peeking** — sequential testing invalidates p-values.
- **Log exposure** — when user first sees the variant.

**After:**
- **Analyze with pre-registered metrics.**
- **Ship the winner** or kill both.
- **Remove the experiment code** after rollout.

**Never:**
- Test 5 things at once without power analysis.
- Declare victory at day 2.
- Ignore guardrails.
- Run experiments on new users only.

## Crash reporting

- **Sentry** (cross-platform, Unity SDK mature).
- **Firebase Crashlytics** (mobile-first, free).
- **Backtrace** (console-friendly).
- **Unity Cloud Diagnostics** (basic, included).

**Rules:**
- **Symbolicate** stack traces (IL2CPP requires symbol upload).
- **Group crashes** by signature.
- **Track crash-free sessions** rate (> 99% target).
- **Alert on regression** in crash rate.

## Analytics implementation

```csharp
public static class Analytics
{
    public static void Track(string evt, params (string key, object val)[] props)
    {
        if (!Consent.AnalyticsAllowed) return;

        var payload = new Dictionary<string, object>(props.Length);
        foreach (var (k, v) in props) payload[k] = v;

        AnalyticsSDK.LogEvent(evt, payload);
    }
}

// Usage
Analytics.Track("level_complete",
    ("level_id", "forest_01"),
    ("time_sec", 145.2f),
    ("deaths", 3),
    ("stars", 2));
```

## Dashboards

- **DAU/MAU** — daily/monthly active users.
- **Retention** — D1, D7, D30 curves.
- **Session length** — average, p50, p95.
- **ARPPU / ARPU** — revenue per user.
- **Funnel** — tutorial completion, first purchase.
- **Crash rate** — crash-free sessions %.

## Anti-patterns

- Logging PII
- No consent flow in GDPR regions
- Events without versioning (rename breaks dashboards)
- Three analytics SDKs
- Remote config without defaults
- Flags without removal dates
- A/B tests without sample size
- Peeking at experiment results
- Ignoring guardrail metrics
- No crash symbolication (useless stack traces)
- Analytics calls scattered across codebase
