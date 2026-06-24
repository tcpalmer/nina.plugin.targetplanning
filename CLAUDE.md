# CLAUDE.md

This file is a guide for coding agents working in 'NINA.Plugin.TargetPlanning.sln'.

## Scope

This guide covers all projects listed in 'NINA.Plugin.TargetPlanning.sln'.

## Implicit Command Approvals

Proceed without asking for confirmation before running any read-only operation — shell commands, web fetches, file reads, build verification. Ask first for anything that writes to disk, modifies remote state, or could be destructive. If the intent is ambiguous, ask.

## Overview

NINA.Plugin.TargetPlanning (aka Target Planning, aka TP) is a plugin for the NINA astrophotography application. Given an imaging target and a date/time, the plugin can compute various details that can determine how suitable the target is for imaging around that date/time. It is the converse of the NINA Sky Atlas: rather than finding targets for one day, it takes one target and shows imaging options (Daily Details, Annual Chart, Imaging Season) across many days. All interaction happens on the plugin page; there are no sequencer instructions.
- It is written in C# exclusively for Windows
- It targets .NET 8 (`net8.0-windows8.0`) and uses Windows Presentation Foundation (WPF) for the user interface
- It is currently built for NINA 3.2 (plugin version 3.2.x — see [`CHANGELOG.md`](CHANGELOG.md))

## Solution Structure

The solution file is nested one level below the repo root at [`NINA.Plugin.TargetPlanning/NINA.Plugin.TargetPlanning.sln`](NINA.Plugin.TargetPlanning/NINA.Plugin.TargetPlanning.sln). The repo root holds `CLAUDE.md`, `README.md`, `CHANGELOG.md`, `LICENSE.txt`, `.editorconfig`, and `.gitignore`.

The solution contains two SDK-style projects, both targeting `net8.0-windows8.0` with `UseWPF`:
- **NINA.Plugin.TargetPlanning** — main plugin. Note the root namespace and output assembly are `TargetPlanning.NINAPlugin`, **not** `NINA.Plugin.TargetPlanning` (the folder/project name differs from the namespace). The plugin entry point is [`TargetPlanning.cs`](NINA.Plugin.TargetPlanning/NINA.Plugin.TargetPlanning/TargetPlanning.cs) (`TargetPlanningPlugin : PluginBase`). A post-build step copies the built DLL into `%localappdata%\NINA\Plugins\` for local testing.
- **NINA.Plugin.TargetPlanning.Test** — NUnit test suite (also `TargetPlanning.NINAPlugin` namespace).

The astrometry logic lives under `Astrometry/` (e.g. `PlanGenerator`, `NighttimeCircumstances`, `OptimalImagingSeason`, solvers); chart/report rendering under `AnnualChart/`, `ImagingSeason/`, and `HTMLOutput/`; WPF views/converters under `Options.xaml`, `Converters/`, and `Resources/`.

## Documentation

- Documentation for NINA is available from https://nighttime-imaging.eu/docs/master/site/.

## NINA Interface

The NINA repository is https://github.com/isbeorn/nina. However, all TS interaction with runtime NINA is via the https://github.com/isbeorn/nina/tree/master/NINA.Plugin package, also available from https://www.nuget.org/packages/NINA.Plugin/, current version 3.2.0.9001.

## Code Style And Formatting

- Maintain the existing line-ending style of every touched file; default to CRLF for new files unless the target location dictates LF.
- Treat the root [`.editorconfig`](.editorconfig) as the canonical C# style source. It covers indentation, line endings, namespace style, `using` placement, `var` preferences, naming, and selected analyzer severities.
- For XAML, follow surrounding file style; no repo-wide XAML formatter configuration is checked in.
- Prefer modern C# supported by the target project (`net8.0-windows8.0`).
- **MVVM pattern in use:** the existing code implements `INotifyPropertyChanged` manually (a `RaisePropertyChanged([CallerMemberName] ...)` helper) and uses NINA's `RelayCommand` / `AsyncCommand<T>` for commands and `AsyncObservableCollection<T>` for bound lists. Match this surrounding style when editing existing types. `CommunityToolkit.Mvvm` is **not currently a referenced package**, so do not introduce `[ObservableProperty]` / `[RelayCommand]` source generators without first adding the package — which requires explicit approval per the dependency rule below. (The `.editorconfig` sets `MVVMTK0034` to error, but that only applies if the toolkit is ever added.)
- Remove any unused `using` directives before saving a file.

## When Making Changes

- **Protected branches**: If the current branch is `main`, refuse all code changes and warn the user. Read-only tasks — analysis, research, design discussion — are permitted on any branch.
- Scope each change to the stated task only. Don't fix unrelated issues, restructure surrounding code, or expand the footprint beyond what was asked.
- Surface ambiguities before writing code rather than making assumptions. One clarifying question upfront beats an unwanted implementation.
- Propose targeted refactorings only when they directly serve the current task, and always call them out explicitly so the user can accept or skip them.
- After any code change, verify the project builds without new errors before reporting the task complete.
- Run affected tests when modifying logic that has test coverage; add tests for new behaviour where a clear test exists nearby to follow.
- Don't add new NuGet package dependencies without explicit approval.

## Git Commits

- Do **not** add a `Co-Authored-By: Claude ...` trailer (or any AI attribution line) to commit messages.
- Keep messages concise: a short imperative subject line, with a bulleted body when the change warrants it.

## Plugin Components

### Testing

Tests live in `NINA.Plugin.TargetPlanning.Test/`.
- **Framework:** NUnit 4 (`[TestFixture]`, `[Test]`, `[SetUp]`)
- **Mocking:** Moq (`Mock<T>`, `.Setup()`, `.Returns()`)
- **Assertions:** FluentAssertions (`.Should()`, `.Be()`, etc.)
- **Native dependencies:** the test project copies native x64 astrometry binaries and data files to the output (`External/x64/NOVAS`, `External/x64/SOFA`, `External/x64/SQLite`, and the `JPLEPH` ephemeris). Tests that exercise astrometry calculations depend on these, so the suite is effectively **x64-only** — run/build it for the x64 platform rather than AnyCPU if astrometry tests fail to load native libraries.
