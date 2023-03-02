# Changelog

## [5.0.9.4] (2023-03-02)

### Added

- [FULF] Allows customization of initial FULF rolling delay ([PR](https://github.com/53m1k0l0n/FFXIV-LazyLoot/pull/22) by [Gidedin](https://github.com/imgidedin)).

## [5.0.9.3] (2023-03-02)

### Changed

- RollDelay can now be setup to a Min and Max value and the delay will be a random value between.

## [5.0.9.2] (2023-02-26)

### Added

- Restrictions to ignore gear which you can't use with your actual job.
- FULF will try to roll if you activate it, in a duty, it's for the people who forget it before they run content. xD

### Changed

- Skip Paladin's and Gladiator Arms, roll them manual.

## [5.0.9.1] (2023-02-24)

### Changed

- FULF will start roll between 1 and 3 seconds

## [5.0.9] (2023-02-23)

### Added

- More restrictions
- A up to 1.5 seconds delay for FULF just to be a bit more Humanlike and no it can't be disabled.

### Changed

- Complete rework of the rolling for FULF
- Changed command from /roll to /rolling because of Kapture

## [5.0.8.2] (2023-02-19)

### Added

- Fix FULF Greed Only to Pass setting - Thanks Gidedin ([PR](https://github.com/53m1k0l0n/FFXIV-LazyLoot/pull/16) by [Gidedin](https://github.com/imgidedin))

## [5.0.8.1] (2023-02-19)

### Added

- Fulf delay configable (Default 5 seconds)

## [5.0.8] (2023-02-18)

### Changed

- From several rolling commands to one command with arguments. /roll need | needonly | greed | pass | passall
- From checking the rolled status after rolling for unidue stuff and Unlockable stuff like mounts and minions to check if you have it already and roll ( Thanks to Gidedin, Midori and LWDUII in SimpleTweaks ).

### Added

- FULF = Fancy Ultimate Lazy Feature or Autoroll, will roll on loot as soon it's available for rolling, like chest opens.
- First restrictions rules for rolling Below Item Lvl and Unlocked Items. ( Thanks to Gidedin )

[Unreleased]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/main...dev
[5.0.9.3]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/v5.0.9.3..v5.0.9.4
[5.0.9.3]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/v5.0.9.2..v5.0.9.3
[5.0.9.2]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/v5.0.9.1..v5.0.9.2
[5.0.9.1]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/v5.0.9..v5.0.9.1
[5.0.9]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/v5.0.8.2..v5.0.9
[5.0.8.2]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/v5.0.8...v5.0.8.2
[5.0.8.1]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/v5.0.8...v5.0.8.1
[5.0.8]: https://github.com/53m1k0l0n/FFXIV-LazyLoot/compare/v5.0.7...v5.0.8
