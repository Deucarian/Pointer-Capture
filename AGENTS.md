# Deucarian Pointer Capture Agent Notes

Package ID: `com.deucarian.pointer-capture`
Repository: `Deucarian/Pointer-Capture`

Follow the canonical Deucarian governance docs in Package Registry, especially capability ownership and dependency rules.

## Ownership

This package owns:

- Cross-platform pointer-lock lifecycle, capture policy, observable capture state, input-neutral rearming, browser bridge behavior, diagnostics, and its package-specific editor management surface.

Registered capabilities:
- `pointer-capture`

This package must not own:

- Camera movement, orbit/fly behavior, drag thresholds, application input mappings, command routing, marker selection, or generic runtime theming.

## Dependencies

- `com.deucarian.editor`: shared Deucarian editor shell used by the package management window.

Runtime code remains input-system agnostic and has no package dependency.

## Policies

- Do not use direct Unity `Debug` calls in production code.
- Keep all validation and fix actions inside `Tools > Deucarian > Interaction > Pointer Capture`; do not add a separate validation menu item.
- Keep platform-specific browser code in the WebGL `.jslib` bridge.
- Test fixture teardown may use `DestroyImmediate` directly.

## Validation

```powershell
python C:/Repositories/Package-Registry/Tools/deucarian_package_validator.py --registry-root C:/Repositories/Package-Registry --repository-root . --config deucarian-package.json
```

Also run Unity EditMode tests and `git diff --check` before committing.

