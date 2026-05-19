# Development Notes

- Use `E:\dnspy\export\Assembly-CSharp` as the primary reference for Against the Storm game code and UI behavior. Check the exported game implementation before guessing at game systems, tooltip layout, recipe availability, services, or panel behavior.
- Keep `TradeRouteDiagnostics/` out of GitHub and mod packages. It is local diagnostic scratch code.
- `dist/` is used for Thunderstore packaging and generated archives. Do not stage generated package outputs unless intentionally changing tracked package metadata.
