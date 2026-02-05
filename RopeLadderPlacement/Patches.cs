using System.Data;
using System.Reflection;
using System.Runtime.CompilerServices;
using HarmonyLib;
using Vintagestory.API.Common;
using Vintagestory.API.MathTools;
using Vintagestory.GameContent;

namespace RopeLadderPlacement;

[HarmonyPatch]
public class Patches {
    private static ModConfig config => ModConfig.Instance;

    // Needed because we call base method
    [HarmonyPatch(typeof(BlockBehavior), nameof(BlockBehavior.OnBlockInteractStart))]
    class OnBlockInteractStartBasePatch {
        [HarmonyReversePatch]
        [MethodImpl(MethodImplOptions.NoInlining)]
        public static bool CallBaseOnBlockInteractStart(BlockBehavior instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling) {
            throw new NoNullAllowedException("This is a stub and should have been replaced by Harmony.");
        }
    }

    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockBehaviorLadder), nameof(BlockBehaviorLadder.OnBlockInteractStart))]
    public static bool InvertOnBlockInteractBlock(BlockBehaviorLadder __instance, IWorldAccessor world, IPlayer byPlayer, BlockSelection blockSel, ref EnumHandling handling, ref bool __result) {
        Block heldBlock = byPlayer.InventoryManager.ActiveHotbarSlot.Itemstack?.Block;
        BlockBehaviorLadder heldLadder = heldBlock?.GetBehavior<BlockBehaviorLadder>();

        if (config.CanPlaceLadderWithRightClick && __instance.isFlexible) {
            if (heldLadder != null && heldLadder.isFlexible) {
                __result = OnBlockInteractStartBasePatch.CallBaseOnBlockInteractStart(__instance, world, byPlayer, blockSel, ref handling);
            } else {
                // We collect when the item is not flex ladder. Right now it's only rope, but i might need to patch if we encounter ladder-{NOT ROPE}-* but isFlexible ladders
                MethodInfo tryLowestMethod = __instance.GetType().GetMethod("TryCollectLowest", BindingFlags.NonPublic | BindingFlags.Instance);
                tryLowestMethod.Invoke(__instance, new object[] { byPlayer, world, blockSel.Position, });
                handling = EnumHandling.PreventDefault;
                __result = true;
            }

            return false;
        } else if (config.CanCollectLadderTopdown && !__instance.isFlexible) {
            handling = EnumHandling.PreventDefault;
        }

        __result = OnBlockInteractStartBasePatch.CallBaseOnBlockInteractStart(__instance, world, byPlayer, blockSel, ref handling);
        return false;
    }


    // Cba extracting the ladder only logic. Just gonna replace it all
    [HarmonyPrefix]
    [HarmonyPatch(typeof(BlockBehaviorLadder), nameof(BlockBehaviorLadder.TryPlaceBlock))]
    public static bool TryPlaceBlock(BlockBehaviorLadder __instance, IWorldAccessor world, IPlayer byPlayer, ItemStack itemstack, BlockSelection blockSel, ref EnumHandling handled, ref string failureCode, ref bool __result) {
        MethodInfo stackUpMethod = __instance.GetType().GetMethod("TryStackUp", BindingFlags.NonPublic | BindingFlags.Instance);
        MethodInfo stackDownMethod = __instance.GetType().GetMethod("TryStackDown", BindingFlags.NonPublic | BindingFlags.Instance);

        handled = EnumHandling.PreventDefault;
        BlockPos pos = blockSel.Position;
        BlockPos aimedAtPos = (blockSel.DidOffset ? pos.AddCopy(blockSel.Face.Opposite) : blockSel.Position);
        Block aboveBlock = world.BlockAccessor.GetBlock(pos.UpCopy());
        string aboveLadderType = aboveBlock.GetBehavior<BlockBehaviorLadder>()?.LadderType;
        if (!__instance.isFlexible && aboveLadderType == __instance.LadderType && __instance.HasSupport(aboveBlock, world.BlockAccessor, pos) && aboveBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
            aboveBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            __result = true;
            return false;
        }

        Block belowBlock = world.BlockAccessor.GetBlock(pos.DownCopy());
        if (belowBlock.GetBehavior<BlockBehaviorLadder>()?.LadderType == __instance.LadderType && __instance.HasSupport(belowBlock, world.BlockAccessor, pos) && belowBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
            belowBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            __result = true;
            return false;
        }

        if (blockSel.HitPosition.Y < 0.5 && (bool)stackDownMethod.Invoke(__instance, new object[] { byPlayer, world, aimedAtPos, blockSel.Face, itemstack })) {
            __result = true;
            return false;
        }

        if ((bool)stackUpMethod.Invoke(__instance, new object[] { byPlayer, world, aimedAtPos, blockSel.Face, itemstack })) {
            __result = true;
            return false;
        }

        if ((bool)stackDownMethod.Invoke(__instance, new object[] { byPlayer, world, aimedAtPos, blockSel.Face, itemstack })) {
            __result = true;
            return false;
        }

        if (__instance.isFlexible && blockSel.Face.IsVertical) {
            failureCode = "cantattachladder";
            __result = false;
            return false;
        }

        AssetLocation blockCode;
        if (blockSel.Face.IsVertical) {
            BlockFacing[] faces = Block.SuggestedHVOrientation(byPlayer, blockSel);
            blockCode = __instance.block.CodeWithParts(faces[0].Code);
        } else {
            blockCode = __instance.block.CodeWithParts(blockSel.Face.Opposite.Code);
        }

        Block orientedBlock = world.BlockAccessor.GetBlock(blockCode);
        if (__instance.HasSupport(orientedBlock, world.BlockAccessor, pos) && orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
            orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            __result = true;
            return false;
        }

        blockCode = __instance.block.CodeWithParts(blockSel.Face.Opposite.Code);
        orientedBlock = world.BlockAccessor.GetBlock(blockCode);
        if (orientedBlock != null && __instance.HasSupport(orientedBlock, world.BlockAccessor, pos) && orientedBlock.CanPlaceBlock(world, byPlayer, blockSel, ref failureCode)) {
            orientedBlock.DoPlaceBlock(world, byPlayer, blockSel, itemstack);
            __result = true;
            return false;
        }

        failureCode = "cantattachladder";
        __result = false;
        return false;
    }
}
