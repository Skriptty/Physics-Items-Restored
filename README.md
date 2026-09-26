# SagTag-Physics-Items-Restored


- BEFORE YOU KEEP ON READING, ALL CREDIT TO VisualError/Ryokune for making the mod, i just restored it up to V81, if you find bugs, make an issue in the GitHub repo or look for the thread in mod releases in the lethal company modding server


### Known Incompatibilities:
- https://github.com/Skriptty/Physics-Items-Restored/labels/compatibility

### Known Bugs/Issues:
- Items may sometimes phase out of existence for the client until picked up by the server when landing the ship.
- Sometimes the items will jumpscare you with its collision sounds. (I have been trying to fix this for hours)
- https://github.com/Skriptty/Physics-Items-Restored/labels/bug


## BUG REPORTING:
- I will only consider bugs reported at: https://github.com/Skriptty/Physics-Items-Restored/issues/new

## Installation

1. Ensure you have [BepInEx](https://thunderstore.io/c/lethal-company/p/BepInEx/BepInExPack/) installed.
2. Download the latest release of the Physics Items mod from [Thunderstore](https://thunderstore.io/c/lethal-company/p/SagTag/Physics_Items_Restored).
3. Extract the contents into your Lethal Company's `BepInEx/plugins` folder.

## Contributing

### Template `Physics_Items/Physics_Items.csproj.user`
```xml
<?xml version="1.0" encoding="utf-8"?>
<Project ToolsVersion="Current" xmlns="http://schemas.microsoft.com/developer/msbuild/2003">
  <PropertyGroup>
    <LETHAL_COMPANY_DIR>C:/Program Files (x86)/Steam/steamapps/common/Lethal Company</LETHAL_COMPANY_DIR>
    <TEST_PROFILE_DIR>$(APPDATA)/r2modmanPlus-local/LethalCompany/profiles/TestPhysicsItems</TEST_PROFILE_DIR>
  </PropertyGroup>

    <!-- Create your 'Test Profile' using your modman of choice before enabling this. 
    Enable by setting the Condition attribute to "true". *nix users should switch out `copy` for `cp`. -->
    <Target Name="CopyToDebugProfile" AfterTargets="PostBuildEvent" Condition="true">
		<MakeDir
                Directories="$(LETHAL_COMPANY_DIR)/BepInEx/plugins/Ryokune-Physics_Items"
                Condition="Exists('$(LETHAL_COMPANY_DIR)') And !Exists('$(LETHAL_COMPANY_DIR)/BepInEx/plugins/Ryokune-Physics_Items')"
        />
		<Copy SourceFiles="$(TargetDir)\$(TargetName).pdb" DestinationFolder="$(LETHAL_COMPANY_DIR)/BepInEx/plugins/Ryokune-Physics_Items" />
		<Exec Command="copy &quot;$(TargetPath)&quot; &quot;$(LETHAL_COMPANY_DIR)/BepInEx/plugins/Ryokune-Physics_Items/&quot;" />
	</Target>
</Project>
```

## Contributors
