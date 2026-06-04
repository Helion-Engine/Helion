# ACS Support

Implements functionality from the [ACS Cross-Port Support Proposal](https://gist.github.com/Gutawer/25d5690569f78ea9ff7f41956ef30472). Features that are not in the cross port support proposal are supported in UZDoom.

##  Script Types

| Script Type | Activator |
|-------------|-----------|
|Open         |World      |
|Enter        |Player     |
|Respawn      |Player     |
|Death        |Player     |

## Opcodes

| Opcode Number(s) | Opcode Function Name | Cross Port | Notes |
|------------------|----------------------|------------|-------|
| 57 + 58          | Random               | ✔ |
| 59 + 60          | ThingCount           | ✔ |
| 61 + 62          | TagWait              | ✔ |
| 63 + 64          | PolyWait             | ✘ |
| 65 + 66          | ChangeFloor          | ✔ |
| 67 + 68          | ChangeCeiling        | ✔ |
| 80               | LineSide             | ✘ |
| 83               | ClearLineSpecial     | ✘ |
| 86               | EndPrint             | ✔ |
| 90               | PlayerCount          | ✘ |
| 91               | GameType             | ✘ |
| 92               | GameSkill            | ✔ |
| 93               | Timer                | ✔ |
| 94               | SectorSound          | ✘ |
| 95               | AmbientSound         | ✘ |
| 97               | SetLineTexture       | ✔ |
| 98               | SetLineBlocking      | ✘ |
| 99               | SetLineSpecial       | ✘ |
| 100              | ThingSound           | ✘ |
| 101              | EndPrintBold         | ✔ |
| 102              | ActivatorSound       | ✘ |
| 103              | LocalAmbientSound    | ✘ |
| 104              | SetLineMonsterBlocking | ✘ |
| 131              | PrintName            | ✘ |
| 132              | SetMusic             | ✔ |
| 138 + 139        | SetGravity           | ✘ |
| 142              | ClearInventory       | ✔ |
| 143 + 144        | GiveInventory        | ✔ |
| 145 + 146        | TakeInventory        | ✔ |
| 147 + 148        | CheckInventory       | ✔ |
| 149 + 150        | Spawn                | ✔ |
| 151 + 152        | SpawnSpot            | ✔ |
| 153 + 154        | SetMusic             | ✔ |
| 155 + 156        | LocalSetMusic        | ✔ | Only implements first two arguments. Order argument is not implemented.
| 174              | Random               | ✔ |
| 180              | SetThingSpecial      | ✔ |
| 196              | GetActorX            | ✔ |
| 197              | GetActorY            | ✔ |
| 198              | GetActorZ            | ✔ |
| 220              | Sin                  | ✔ |
| 221              | Cos                  | ✔ |
| 222              | VectorAngle          | ✔ |
| 247              | PlayerNumber         | ✘ |
| 248              | ActivatorTID         | ✘ |
| 259              | GetActorFloorZ       | ✔ |
| 260              | GetActorAngle        | ✔ |
| 261              | GetSectorFloorZ      | ✔ |
| 262              | GetSectorCeilingZ    | ✔ |
| 282              | GetActorCeilingZ     | ✔ |
| 283              | SetActorPosition     | ✔ |
| 288              | ThingCountName       | ✔ |
| 289              | SpawnSpotFacing      | ✔ |
| 327              | ChangeLevel          | ✘ |
| 342              | ThingCountSector     | ✔ |
| 343              | ThingCountNameSector | ✔ |

## Functions

| Func Number | Function Name | Cross Port |
|-------------|---------------|------------|
| 1           | GetLineUDMFInt       | ✔ |
| 2           | GetLineUDMFFixed     | ✔ |
| 3           | GetThingUDMFInt      | ✔ |
| 4           | GetThingUDMFFixed    | ✔ |
| 5           | GetSectorUDMFInt     | ✔ |
| 6           | GetSectorUDMFFixed   | ✔ |
| 7           | GetSideUDMFInt       | ✔ |
| 8           | GetSideUDMFFixed     | ✔ |
| 9           | GetActorVelX         | ✔ |
| 10          | GetActorVelY         | ✔ |
| 11          | GetActorVelZ         | ✔ |
| 20          | SpawnSpotForced      | ✔ |
| 21          | SpawnSpotFacingForced | ✔ |
| 23          | SetActorVelocity     | ✔ |
| 36          | SpawnForced          | ✔ |
| 46          | UniqueTID            | ✔ |
| 47          | IsTIDUsed            | ✔ |
| 48          | Sqrt                 | ✔ |
| 49          | FixedSqrt            | ✔ |
| 50          | VectorLength         | ✔ |
| 207         | Floor                | ✔ |
| 208         | Round                | ✔ |
| 209         | Ceil                 | ✔ |
| 300         | GetLineX             | ✔ |
| 301         | GetLineY             | ✔ |
| 401         | SetFogDensity        | ✘ |
