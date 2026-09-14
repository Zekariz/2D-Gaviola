# Contributing to 2D-Gaviola

First off — thank you for taking the time to contribute! 🎉

This document outlines how to propose changes, report bugs, and submit
pull requests. Following these guidelines keeps the review process smooth
for everyone.

---

## 📋 Table of Contents

- [Code of Conduct](#code-of-conduct)
- [Getting Started](#getting-started)
- [How Can I Contribute?](#how-can-i-contribute)
- [Branch Strategy](#branch-strategy)
- [Commit Conventions](#commit-conventions)
- [Pull Request Process](#pull-request-process)
- [Unity-Specific Rules](#unity-specific-rules)
- [Style Guide](#style-guide)

---

## Code of Conduct

By participating, you agree to abide by our
[Code of Conduct](CODE_OF_CONDUCT.md). Please report unacceptable behavior
to our project maintainer.

---

## Getting Started

### Prerequisites

- **Unity 6000.3.24f1 LTS** (install via [Unity Hub](https://unity.com/download))
- **Git** and **Git LFS** — see [README](README.md) for setup
- **Rider** or **Visual Studio** (recommended)

### Setup

```bash
git clone https://github.com/Zekariz/2D-Gaviola.git
cd 2D-Gaviola
git lfs install
git lfs pull
```

Open the project in Unity Hub. First import will regenerate `Library/`
and take a few minutes.

### Unity project settings (required)

Before editing code, confirm in **Edit → Project Settings → Editor**:

- **Version Control Mode:** `Visible Meta Files`
- **Asset Serialization Mode:** `Force Text`

These ensure clean diffs and prevent asset corruption.

---

## How Can I Contribute?

### 🐛 Reporting Bugs

Open an issue using the **Bug Report** template. Include:

- Unity version + OS
- Steps to reproduce
- Expected vs. actual behavior
- Screenshots/video if visual
- Relevant Console logs

### 💡 Suggesting Features

Open an issue using the **Feature Request** template. Explain the **why**,
not just the **what**.

### 📝 Improving Documentation

Doc-only PRs are always welcome — typos, clarifications, examples.

### 🎨 Contributing Art / Audio

Open an issue first so we can agree on style and licensing before you
invest hours. All contributions must be your own work or CC0.

---

## Branch Strategy

| Branch | Purpose |
|---|---|
| `main` | Stable, release-ready |
| `develop` | Active integration — PRs target this |
| `feature/<name>` | New features |
| `bugfix/<name>` | Non-critical fixes |
| `hotfix/<name>` | Urgent fixes from `main` |

**Always branch from `develop`** (not `main`) unless you're doing a hotfix.

```bash
git checkout develop
git pull
git checkout -b feature/my-awesome-thing
```

---

## Commit Conventions

We use [Conventional Commits](https://www.conventionalcommits.org/):

```
<type>(<scope>): <description>

[optional body]

[optional footer]
```

**Types:** `feat`, `fix`, `chore`, `docs`, `style`, `refactor`, `perf`, `test`, `build`, `ci`

**Examples:**

```
feat(player): add double-jump ability
fix(ui): resolve HUD overlapping minimap on ultrawide
chore: bump Unity to 2022.3.40f1
docs(readme): update setup instructions
refactor(enemy): extract pathfinding into service
```

Keep the subject line ≤ 72 chars, imperative mood, no trailing period.

---

## Pull Request Process

1. **Branch from `develop`** and work in small, focused commits.
2. **Rebase** onto latest `develop` before opening the PR:
   ```bash
   git fetch origin
   git rebase origin/develop
   ```
3. **Fill in the PR template** — describe what changed and why.
4. **Link the issue** with `Closes #123` in the description.
5. **Ensure the CI passes** (build + tests).
6. **Request review** from a maintainer.
7. **Squash-merge** once approved — one PR = one commit on `develop`.

PRs that touch >500 lines should be split. If you must submit a large PR,
explain why in the description.

---

## Unity-Specific Rules

These are non-negotiable — violating them corrupts the repo:

1. **NEVER commit `Library/`, `Temp/`, `Obj/`, `Logs/`, or `UserSettings/`.**
2. **ALWAYS commit `.meta` files** alongside their assets. A missing
   `.meta` breaks references for everyone else.
3. **ALWAYS use Git LFS** for binaries — it's configured in
   `.gitattributes`; don't override it.
4. **DON'T rename or move assets** in a PR unless that's the point of the PR.
   Renames break references across scenes/prefabs.
5. **DON'T edit `.unity` or `.prefab` files by hand** unless you know what
   you're doing — use Unity's editor.
6. **DO use `[SerializeField] private`** instead of `public` fields.
7. **DON'T commit generated `.csproj` / `.sln` files** — Unity regenerates
   them.

---

## Style Guide

We follow the repo's `.editorconfig` (Allman braces, 4-space indent,
`_camelCase` private fields). Run your IDE's formatter before committing.

Additional conventions:

- **Namespaces:** `YourGame.<Module>` — e.g., `YourGame.Player`
- **Interfaces:** prefix `I` — e.g., `IDamageable`
- **ScriptableObjects:** suffix `Data` — e.g., `WeaponData`
- **Prefabs:** PascalCase matching the class name
- **Avoid `Find`/`FindObjectOfType`** in `Update` — cache references
- **Use `[RequireComponent]`** for mandatory dependencies

---

## Questions?

Open a Discussion.

Thanks again! 💜
