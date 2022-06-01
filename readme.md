# Stellaris patcher

This application will patch Stellaris game so it will allow you to get achievements even with active mods.
Just run it and follow instructions. It _should_ work for any Stellaris version including future ones, unless developers will intentionally change the code to break it.

You can also use it through CLI:

```
StellarisPatcher.exe --help
StellarisPatcher.exe C:/path/to/stellaris.exe
StellarisPatcher.exe C:/path/to/stellaris.exe C:/path/to/resulting/stellaris.exe
```

## How it works

There are multiple places in game where hash-sum is checked using `strcmp` function. Patcher will find all `strcmp` calls that uses constant string as first argument and replace it with same value as actual hash, so it will always match.

### Step 1

Find following pattern (based on AsterAgain's [post](https://www.reddit.com/r/StellarisMods/comments/n007f3/comment/gw6z6d2/?utm_source=share&utm_medium=web2x&context=3)):

```asm
; 48 8B 12 
mov rdx [rdx]

; 48 8D 0D ?? ?? ?? ?? 
lea rcx [COMPILED_HASH_ADDRESS] ; (some relative location, dependent on platform/binary - this relative location is the location of the actual hashsum, which is also stored in the binary)

; E8 
call dword ; (some absolute location, dependent on platform/binary - this location will contain the assembly of the C function strcmp)
```

For my version 3.4.3 it is only 14 calls that match that pattern, but we have to filter ones that we actually need. 

### Step 2

Find what string is actually referenced by `lea rcx <addr>` 

### Step 3

Try to find what string is hash based on whether or not it is fully alphanumeric and have more than 1 `strcmp` call to it. If there are more that one potential candidate, ask user to select actual version hash.

### Step 4

Now, when we have locations for all those checks, we can do some patching. In pseudo-code it does something like this:
```c++
strcmp("<compiled hash>", actualHash) --> strcmp(actualHash, actualHash)
```

In actual assembly changes this:
```asm
mov rdx, qword ptr [rdx]
lea rcx, [COMPILED_HASH_ADDRESS]
call strcmp 
```

to this:

```asm
mov rdx, qword ptr [rdx]
mov rcx, rdx
nop
nop
nop
nop
call strcmp 
```
