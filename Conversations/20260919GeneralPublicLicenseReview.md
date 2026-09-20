# Review changing to GPL-3.0-or-later

Date: 2026-09-19

## User

i want to change the license of this repository to GPL-3.0-or-later. is there any issue with that?

## Assistant

I’ll check the current license, contributor history, and any bundled third-party material for potential obstacles to switching to GPL-3.0-or-later.

## Assistant

The current license names you as the copyright holder, and all 171 commits in the local history are attributed to you. I haven’t found an obvious blocker. The main decision is scope: whether GPL should cover just the code or also the models, artwork, and lore. It also permits recipients to redistribute the game, including commercially.

## Assistant

**I found no obvious blocker to switching to GPL-3.0-or-later**, assuming you own the rights to the repository’s original content.

The current [License](/C:/Users/k/Repository/Automatou/License) names you as copyright holder, and all 171 commits in the available history are attributed to you. That supports a straightforward change, though Git history cannot establish ownership of copied material or work covered by employment agreements. Other owners’ material would need permission or compatible licensing. [FSF guidance](https://www.fsf.org/blogs/licensing/relicensing-versus-compatibility).

The main implications are:

- **You can still sell the game, but recipients can redistribute it—including free copies or commercial forks.** GPL therefore changes the exclusivity of your earlier proprietary-game plan. Distributed derivatives generally must remain GPL-covered, with corresponding source available under the license’s requirements. [GNU FAQ](https://www.gnu.org/licenses/gpl-faq.en.html#GPLCommercially).
- **Released GPL rights cannot simply be withdrawn.** Compliant recipients keep their rights to released versions. “Or later” also lets recipients choose future GPL versions published by the FSF. [GPL sections 2 and 14](https://www.gnu.org/licenses/gpl.en.html).
- **Be explicit about assets and lore.** This repository includes Blender models, documentation, lore, and conversation logs. Decide whether those are included or separately licensed. GPL can cover artwork too, but its editable source—such as the original `.blend` files—matters. [GNU FAQ](https://www.gnu.org/licenses/gpl-faq.en.html#GPLOtherThanSoftware).
- **Preserve third-party notices.** Godot and the .NET runtime use GPL-compatible MIT licensing, so those core dependencies are not an apparent obstacle. Their notices and bundled components’ terms still apply; this review was not a complete audit of exported binaries. [Godot licensing](https://docs.godotengine.org/en/stable/about/complying_with_licenses.html), [.NET license](https://github.com/dotnet/runtime/blob/main/LICENSE.TXT), [GNU compatibility list](https://www.gnu.org/licenses/license-list.html.en#Expat).

The practical change would replace the proprietary license and explicitly state **GPL version 3 or any later version** for the intended scope. [GNU application instructions](https://www.gnu.org/licenses/gpl-howto.en.html).

The license is unchanged. I recorded and committed this discussion as required by the repository instructions.

