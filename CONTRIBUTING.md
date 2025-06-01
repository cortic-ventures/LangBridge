## Dev setup

1. `git clone` → `dotnet build` (SDK 8.0)
2. Run tests: `dotnet test`

## Branching Strategy & Pull Requests

We follow a simplified Git workflow to ensure a stable `main` branch and clear development paths.

### Main Branch (`main`)
*   The `main` branch is always stable and represents the latest released version of the library.
*   All merges into `main` must come from completed, reviewed feature branches or hotfix branches.

### Feature Branches (`feat/<topic>`)
*   All new features, especially those introducing new public API surfaces or significant changes, are developed on dedicated feature branches.
*   Keep feature branches focused and as small as possible.
*   Merge into `main` only when the feature is complete, thoroughly tested, and ready for public consumption.
*   If a feature is abandoned, its branch can be closed or deleted without impacting `main`.

### Hotfix Branches (`hotfix/<topic>`)
*   For urgent bug fixes that need to be applied directly to the `main` branch.

### Pull Requests (PRs)
*   All changes must be submitted via Pull Requests.
*   PRs should be kept under 400 Lines of Code (LOC) where possible, and include relevant unit tests.
*   Adhere to Conventional Commits 1.0.0: [https://www.conventionalcommits.org/en/v1.0.0/](https://www.conventionalcommits.org/en/v1.0.0/)
*   All feature branches must be squashed into a single commit before merging into `main`.

## Coding style

* Nullable reference types **enabled**
* `async` all I/O methods.
