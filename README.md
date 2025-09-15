# DungeonRunner

## Animator
### Omnimovement Layer
- Layer for 2D freeform directional animation blend tree
### Upper Body
- Layer for blending animations with IKs
### Lower Body
- Layer for blending animations with IKs
### General notes
- The animator is utilized by capturing input using C# scripts then, distributed to animator parameters. 
- Animator parameters are values being updated by scripts to then utilized by the animator for transition state logic.

For example...

```
anim.SetFloat("Speed", speed, animDamp, Time.deltaTime);
anim.SetFloat("MoveX", localMove.x, animDamp, Time.deltaTime);
anim.SetFloat("MoveY", localMove.z, animDamp, Time.deltaTime);
```

...or...

```
animator.SetBool("Attack", true);
```

These values can be used when creating transitions from state to state.

## Multiplayer
### Local
- Project is equipped with local multiplayer. Pressing any control from any assigned input will spawn a player prefab.

```
