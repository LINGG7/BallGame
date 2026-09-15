# Effect skin checks

- Runtime and editor C# compiled with the installed Unity 2022.3 compiler and project references.
- ServiceTests runs the real EffectSkinService source against in-memory Unity boundary doubles: 17 assertions pass. It does not alter player save data.
- Unity created and imported all four effect prefabs, their previews/materials, the catalog, and the ShopModal category tab. Reference validation passed.
- Editor integration checked appearance → effects → appearance → effects without duplicate cards and checked Chinese glyph availability.
- shop-effects.png is an inspected Unity render of the actual shop prefab/controller.
- trails.png is an unreliable edit-mode capture (only the balls were captured); it must not be treated as proof that runtime trails work. In-game verification is separate.
- A real IRewardedAdService adapter is not currently registered in this project. Paid-by-ad effect skins remain locked when the provider is unavailable. Basic is free.

Run the isolated service checks:
dotnet run --project BuildChecks/EffectSkins/ServiceTests/ServiceTests.csproj

The approved-build.request and visual-check.request files are one-shot, ignored local requests. Never re-arm asset generation after assets exist. The prefab builder deliberately refuses to overwrite existing effect resources.