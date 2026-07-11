# Line Specials

## Line

| Number | Id |
|--------|-------------------|
| 55 | Line_SetBlocking |

## Door

| Number | Id |
|--------|-------------------|
| 202 | Generic_Door |
| 249 | Door_CloseWaitOpen |

## Floor

| Number | Id |
|--------|--------------------------------|
| 37 | Floor_MoveToValue |
| 200 | Generic_Floor |
| 235 | Floor_TransferTrigger |
| 236 | Floor_TransferNumeric |
| 238 | Floor_RaiseToLowestCeiling |
| 239 | Floor_RaiseByValueTxTy |
| 240 | Floor_RaiseByTexture |
| 241 | Floor_LowerToLowestTxTy |
| 242 | Floor_LowerToHighest |
| 250 | Floor_Donut |
| 251 | FloorAndCeiling_LowerRaise |

## Stairs

| Number | Id |
|--------|----------------------|
| 204 | Generic_Stairs |
| 217 | Stairs_BuildUpDoom |

## Ceiling

| Number | Id |
|--------|--------------------------------|
| 97 | Ceiling_LowerAndCrushDist |
| 104 | Ceiling_CrushAndRaiseSilentDist |
| 47 | Ceiling_MoveToValue |
| 169 | Generic_Crusher2 |
| 192 | Ceiling_LowerToHighestFloor |
| 193 | Ceiling_LowerInstant |
| 194 | Ceiling_RaiseInstant |
| 195 | Ceiling_CrushRaiseAndStayA |
| 196 | Ceiling_CrushAndRaiseA |
| 197 | Ceiling_CrushAndRaiseSilentA |
| 201 | Generic_Ceiling |
| 205 | Generic_Crusher |
| 252 | Ceiling_RaiseToNearest |
| 253 | Ceiling_LowerToLowest |
| 254 | Ceiling_LowerToFloor |
| 255 | Ceiling_CrushRaiseAndStaySilA |

## Transfer

| Number | Id |
|--------|---------------------------|
| 50 | ExtraFloor_LightOnly |
| 209 | Transfer_Heights |
| 210 | Transfer_FloorLight |
| 211 | Transfer_CeilingLight |

## Platform

| Number | Id |
|--------|-------------------------------|
| 172 | Plat_UpNearestWaitDownStay |
| 203 | Generic_Lift |
| 206 | Plat_DownWaitUpStayLip |
| 207 | Plat_PerpetualRaiseLip |
| 228 | Plat_RaiseAndStayTx0 |
| 230 | Plat_UpByValueStayTx |
| 231 | Plat_ToggleCeiling |

## Teleport

| Number | Id |
|--------|---------------------------|
| 39 | Teleport_ZombieChanger |
| 76 | TeleportOther |
| 77 | TeleportGroup |
| 78 | TeleportInSector |
| 154 | Teleport_NoStop |
| 215 | Teleport_Line |

## Thing

| Number | Id |
|--------|---------------------------|
| 17 | Thing_Raise |
| 19 | Thing_Stop |
| 119 | Thing_Damage |
| 125 | Thing_Move |
| 127 | Thing_SetSpecial |
| 128 | ThrustThingZ |
| 139 | Thing_SpawnFacing |
| 175 | Thing_ProjectileIntercept |
| 176 | Thing_ChangeTID |
| 177 | Thing_Hate |
| 178 | Thing_ProjectileAimed |
| 248 | HealThing |

## End

| Number | Id |
|--------|----------------|
| 243 | Exit_Normal |
| 244 | Exit_Secret |

## Scroll

| Number | Id |
|--------|---------------------------|
| 222 | Scroll_Texture_Model |
| 223 | Scroll_Floor |
| 224 | Scroll_Ceiling |

## Light

| Number | Id |
|--------|---------------------------|
| 117 | Light_Stop |
| 232 | Light_StrobeDoom |
| 233 | Light_MinNeighbor |
| 234 | Light_MaxNeighbor |

---

# Properties

## Skills
- skill1
- skill2
- skill3
- skill4
- skill5

## Linedef
- comment
- health
- healthgroup
- damagespecial
- deathspecial
- arg0str
- alpha
- locknumber

## Sidedef
- comment
- scalex_mid
- lightabsolute
- offsetx_top
- scalex_bottom
- offsety_bottom
- offsetx_bottom
- scaley_bottom
- light
- offsetx_mid
- offsety_top
- scaley_top
- scaley_mid
- offsety_mid
- scalex_top
- light_top
- lightabsolute_top
- light_mid
- lightabsolute_mid
- light_bottom
- lightabsolute_bottom

## Thing
- comment
- gravity
- alpha
- health
- arg0str

## Sector
- comment
- ypanningfloor
- xpanningfloor
- lightfloorabsolute
- lightfloor
- lightcolor
- fadecolor
- fogdensity
- damageinterval
- rotationceiling
- damageamount
- rotationfloor
- yscalefloor
- leakiness
- ypanningceiling
- lightceiling
- yscaleceiling
- gravity
- xpanningceiling
- xscaleceiling
- xscalefloor
- lightceilingabsolute
- xscrollfloor
- yscrollfloor
- scrollfloormode
- xscrollceiling
- yscrollceiling
- scrollceilingmode
- skyfloor
- skyceiling
- frictionfactor
- movefactor

## Linedef Flags

- twosided
- dontpegtop
- dontpegbottom
- blocking
- blockeverything
- blockplayers
- blockmonsters
- blocklandmonsters
- blockfloaters
- blocksound
- blockprojectiles
- blockhitscan
- blockuse
- blocksight
- jumpover
- clipmidtex
- wrapmidtex
- midtex3dimpassible
- midtex3d
- mapped
- secret
- dontdraw
- transparent
- translucent
- monsteractivate

## Linedef Activations

- repeatspecial
- playeruse
- playercross
- playerpush
- monsteruse
- monstercross
- monsterpush
- anycross
- missilecross
- impact
- checkswitchrange
- passuse
- firstsideonly
- playeruseback

## Sidedef Flags

- clipmidtex
- wrapmidtex
- smoothlighting
- nofakecontrast

## Thing Flags

- skill1
- skill2
- skill3
- skill4
- skill5
- single
- coop
- dm
- friend
- ambush
- dormant
- translucent
- invisible
- countsecret
