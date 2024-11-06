https://kybernetik.com.au/ultevents/docs/changes

> When updating plugins, you must delete any previous version from your project first. This is mandatory since Unity's package importer system doesn't delete or rename existing files so any old scripts that aren't directly overwritten will cause compile errors that prevent anything from working.

# UltEvents 3.0.3

- 2024-08-17
- Un-`sealed` `UltEvent`. All the generic versions were already un-`sealed` and this allows Animancer to have an inheriting class which can be used for Animancer Event callbacks.
- Changed the Example scene into a hidden Sample which can be imported via the Package Manager.
- Changed the type picker menu to support context menu style in Unity 2023 since it's now searchable and scrollable.
- Added `IUltEvent.Invoke`.
- Added `UnityEventCompatibility` containing extension methods for `AddCallback` and `RemoveCallback` to give `UltEvents` an API like `UnityEvents`.
- Added conditional compilation symbols for `UNITY_PHYSICS_3D` and `2D` in case those modules have been disabled.
- Fixed `UltEventUtils.GetPlacementName` to give the proper values beyond "3rd".
- Fixed potential `NullReferenceException` in `UltEventBase.ToString`.

# UltEvents 3.0.2

- 2024-01-04
- Changed the type picker menu to use `GetNameCS` instead of `FullName` so that primitive aliases like `float` can be used instead of needing their full names (`System.Single`).
- Fixed `PersistentCallDrawer.GetSupportedTypes` to ignore dynamic assemblies instead of throwing an exception.

# UltEvents 3.0.1

- 2024-01-03
- Fixed build error in `PersistentCall`.

# UltEvents 3.0.0

- 2024-01-01
- Features:
  - Added support for getting and setting fields directly.
  - Replaced the method selection menu with an `AdvancedDropdown` which has a nicer layout and inbuilt search bar.
    - The old context menu style can be reactivated by selecting "Display Options -> Context Menu Style".
  - Added `Operators` for simple math.
  - Added `ReflectionCache` for more efficient initialization of events that reuse the same types for static member access.
  - Removed the `UltEventsUtils.InvokeX` extension methods since you can just use `ultEvent?.Invoke();`.
  - Improved the UltEvent Inspector to highlight the event header in red if any of its calls are invalid so you can find problems easier if they're collapsed.
  - Improved the "Attempted to Invoke a PersistentCall which couldn't find it's method" warning to include the name of the method it was looking for and mention Unity's [Script Stripping](https://docs.unity3d.com/Manual/ManagedCodeStripping.html) in Runtime Builds.
- Housekeeping:
  - Increased the minimum supported Unity version to 2021.3.
  - Moved from Assets/Plugins to the Packages folder so UltEvents can be referenced by other Packages.
    - Renamed the Assembly from `UltEvents` to `Kybernetik.UltEvents` to match package assembly naming guidelines.
    - If you have any Assembly Definitions which reference a previous version of `UltEvents`, make sure they have "Use GUIDs" enabled before upgrading. Otherwise, you will need to re-assign those references.
    - Added a separate `Kybernetik.UltEvents.Editor` assembly for Editor-Only stuff.
  - Updated code to use newer C# features: expression-bodied members, null-coalescing operators, out parameter declarations, range operators, string interpolation.
- Fixes:
  - Fixed `Serialization` system to better handle `[SerializeReference]` fields.
  - Fixed `MethodSelectionMenu` headings to respect `BoolPref.ShowFullTypeNames`.
  - Fixed call reordering to not break linked return values.
  - Fixed constructors to work properly.

# UltEvents 2.2.0

- 2021-08-20
- Increased the minimum supported Unity version to 2018.4.
- Improved the `Serialization` script:
  - Improved `Serialization.GetValue` and `SetValue` to directly access `Character` and `Gradient` properties.
  - Changed `Serialization.PropertyAccessor.Field` and `FieldType` to be private and added `GetField` and `GetFieldElementType` methods so that it can support `[SerializeReference]` fields where inheritance might prevent the `FieldInfo` from being accessible just based on the field type.
  - Added support for inheritance in `[SerializeReference]` to the `Serialization` system.
  - Added `Serialization.CopyValueFrom`.
  - Added `IsDefaultValueByType` and `ResetValue`.
  - Renamed `ArrayPropertyAccessor` to `CollectionPropertyAccessor` and added methods for accessing the collection itself rather than the target item.
  - Fixed errors when trying to get or set the value of a property with a null object somewhere in its chain.
  - Fixed `Serialization.GetValue` to work properly when the property has multiple different values.
  - Fixed `Serialization.PropertyAccessor.ResetValue` to run the constructor of the field's current type so that it can reset `[SerializeReference]` fields to the defaults of the current type instead of null.
 
# UltEvents 2.1.0

- 2020-03-06
- Added `UltEventBase.DynamicInvoke` which takes an `object[]`.
- Added a display option for "Use Indentation" to optionally fill the full Inspector width instead of indenting properly.
- Flipped the `Get` and `Set` toggles for properties. `Get` is now shown when the getter is selected rather than as a button to change to the getter.
- Fixed exception caused by the UI Elements system when resizing arrays containing UltEvents.
- Fixed a few GUI spacing issues in Unity 2019.3+.
- Fixed the `Click to add a listener` label to not disappear when opening the context menu.
- Refactored `SerializedPropertyReference` into `Serialization.PropertyReference`.
 
# UltEvents 2.0.0

- 2019-06-03
- Moved everything out of the precompiled DLL to make it easier to access and modify the source code.

> **Warning**: when upgrading from an earlier version you must delete the old version before importing the new one. This will also cause all of the [Premade Event Scripts](creating-and-triggering#premade-event-scripts) in your project to go missing so you will need to set them all up again. This is an unfortunate side effect of the way Unity handles references to scripts inside a DLL compared to regular script files.

- Replaced PDF user manual with a website hosted at kybernetik.com.au/ultevents.
- Added support for constructors.
- Added interfaces corresponding to all `UltEvent` types so that the ability to add and remove listeners can be exposed without exposing the ability to invoke, clear, or access other members of the event.
- Fixed cached `PersistentArgument` values to be cleared properly when the user modifies the argument in the Inspector.
- Removed the Parameter Constructors sub-menu because it isn't particularly useful now that actual constructors are supported.

# UltEvents 1.2.0

- 2018-09-11
- Fixed an issue where persistent arguments using parameters or returned values would cache the first value they were given and keep using that.
- Changed structure of source code project back to having the Runtime project link all the files in the Editor project. Shared Projects are more hassle than they're worth.

# UltEvents 1.1.0

- 2018-07-21
- Fixed invocation to not allocate garbage every time for value type parameters.
- Changed structure of source code project to use a Shared Project instead of having the Runtime project link all the files in the Editor project.

# UltEvents 1.0.0

- 2018-07-10
- Initial release.
