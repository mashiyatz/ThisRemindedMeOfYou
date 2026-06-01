import bpy, sys

# patch FBX importer for Blender 5.1
try:
    bpy.ops.import_scene.fbx(filepath=__file__)
except Exception:
    pass
for name, mod in list(sys.modules.items()):
    if 'import_fbx' in name and hasattr(mod, 'blen_read_light'):
        _orig = mod.blen_read_light
        def _safe(a, b, c, _f=_orig):
            try: return _f(a, b, c)
            except AttributeError: return None
        mod.blen_read_light = _safe

bpy.ops.object.select_all(action='SELECT')
bpy.ops.object.delete(use_global=False)

src = r"C:\Users\mashi\Desktop\ThisRemindedMeOfYou\CornellBox\Assets\ReadingRoom\Mesh\allmesh.FBX"
bpy.ops.import_scene.fbx(filepath=src)

print("=== OBJECTS IN allmesh.FBX ===")
for obj in sorted(bpy.data.objects, key=lambda o: o.name):
    print(f"  {obj.type:8} | {obj.name}")
print("=== END ===")
