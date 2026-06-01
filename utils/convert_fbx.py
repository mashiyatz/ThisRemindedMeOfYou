"""
FBX → GLB batch converter for ThisRemindedMeOfYou.
Run headlessly: blender --background --python convert_fbx.py
"""
import bpy
import sys
import os

REPO   = r"C:\Users\mashi\Desktop\ThisRemindedMeOfYou"
ASSETS = os.path.join(REPO, "CornellBox", "Assets")
OUT    = os.path.join(REPO, "web", "public", "models")

os.makedirs(OUT, exist_ok=True)

# ── Blender 5.1 workaround ─────────────────────────────────────────────────
# The FBX importer tries to set lamp.cycles.cast_shadow which was removed.
# Force-load the importer module, then patch blen_read_light to swallow the
# AttributeError so lights are simply skipped rather than crashing the import.
def _patch_fbx_importer():
    # Trigger addon load by running a no-op import attempt
    try:
        bpy.ops.import_scene.fbx(filepath=__file__)  # will fail fast, but loads module
    except Exception:
        pass
    for name, mod in list(sys.modules.items()):
        if 'import_fbx' in name and hasattr(mod, 'blen_read_light'):
            _orig = mod.blen_read_light
            def _safe(fbx_tmpl, fbx_obj, settings, _f=_orig):
                try:
                    return _f(fbx_tmpl, fbx_obj, settings)
                except AttributeError:
                    return None
            mod.blen_read_light = _safe
            print(f"[PATCH] blen_read_light patched in {name}")
            return True
    return False

_patch_fbx_importer()

# ── Targets ────────────────────────────────────────────────────────────────
# Each entry: (rel_path, out_name, keep_only_names_or_None)
# keep_only_names: set of object name prefixes to keep (others deleted). None = keep all.
TARGETS = [
    (r"FurnitureAssets\Bookshelves\shkaff\shkaff.fbx",                 "shkaff.glb",   None),
    (r"FurnitureAssets\V2_Assets\Table\round_wooden_table_01_4k.fbx",  "table.glb",    None),
    (r"FurnitureAssets\Curtain.fbx",                                    "curtain.glb",  None),
    (r"FurnitureAssets\PD HousePlants\Models\Plant 6.fbx",             "plant.glb",    None),
    (r"FurnitureAssets\Azerilo\Free Rug Pack\Mesh\Rug.fbx",            "rug.glb",      None),
    (r"ShelfObjects\ShelfSet.fbx",                                      "shelfset.glb", None),
    (r"FurnitureAssets\3dizart Books Pack\Models\Book.FBX",            "book.glb",     None),
    # New V2 assets
    (r"ReadingRoom\Mesh\chair.FBX",                                     "chair.glb",    None),
    # allmesh.FBX contains the whole room; export only the shoji window group.
    # Window objects: frame box + three paper-pane planes (the fourth is inactive).
    (r"ReadingRoom\Mesh\allmesh.FBX",                                   "window.glb",
        {"Box006", "Plane038", "Plane040", "Plane038.001"}),
    # Dark-wood corner insert between the left wall and back wall.
    (r"ReadingRoom\Mesh\allmesh.FBX",                                   "wallcorner.glb",
        {"WallCorn ins", "WallCorn_ins"}),
]

# ── Conversion loop ────────────────────────────────────────────────────────
results = []

for rel, out_name, keep_only in TARGETS:
    src = os.path.join(ASSETS, rel)
    out = os.path.join(OUT, out_name)

    if not os.path.exists(src):
        results.append(f"SKIP  {out_name}  (not found: {rel})")
        continue

    # Clear scene
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

    try:
        bpy.ops.import_scene.fbx(filepath=src)
    except Exception as exc:
        results.append(f"ERROR {out_name}  import: {exc}")
        continue

    # Remove lights — not needed for web rendering
    for obj in list(bpy.data.objects):
        if obj.type == 'LIGHT':
            bpy.data.objects.remove(obj, do_unlink=True)

    # If a name filter was given, delete everything that doesn't match.
    # Blender may suffix duplicate names with .001, .002 etc.; we strip those
    # and compare against the base name.
    if keep_only is not None:
        def _base(name):
            # strip Blender's numeric suffix (.001, .002, …)
            import re
            return re.sub(r'\.\d+$', '', name)

        to_delete = [
            obj for obj in bpy.data.objects
            if obj.name not in keep_only and _base(obj.name) not in keep_only
        ]
        for obj in to_delete:
            bpy.data.objects.remove(obj, do_unlink=True)

    if not any(o.type == 'MESH' for o in bpy.data.objects):
        results.append(f"ERROR {out_name}  no mesh objects after filtering (keep={keep_only})")
        continue

    try:
        bpy.ops.export_scene.gltf(
            filepath=out,
            export_format='GLB',
            export_apply=True,
        )
        results.append(f"OK    {out_name}")
    except Exception as exc:
        results.append(f"ERROR {out_name}  export: {exc}")

# ── OBJ targets (no MTL — textures applied manually) ──────────────────────
# Each entry: src OBJ path, output GLB name, dict of texture role → path.
# Roles: albedo, normal, gloss (inverted → roughness), specular, ao.
_CHAIR_TEX = os.path.join(ASSETS, r"FurnitureAssets\chair")
OBJ_TARGETS = [
    {
        'src':  os.path.join(_CHAIR_TEX, "chair_low.obj"),
        'out':  os.path.join(OUT, "chair_low.glb"),
        'textures': {
            'albedo':   os.path.join(_CHAIR_TEX, "chair_albedo.png"),
            'normal':   os.path.join(_CHAIR_TEX, "chair_normal.png"),
            'gloss':    os.path.join(_CHAIR_TEX, "chair_gloss.png"),
            'specular': os.path.join(_CHAIR_TEX, "chair_specular.png"),
            'ao':       os.path.join(_CHAIR_TEX, "chair_ao.png"),
        },
    },
]

for entry in OBJ_TARGETS:
    src      = entry['src']
    out      = entry['out']
    out_name = os.path.basename(out)
    textures = entry.get('textures', {})

    if not os.path.exists(src):
        results.append(f"SKIP  {out_name}  (not found: {src})")
        continue

    # Clear scene
    bpy.ops.object.select_all(action='SELECT')
    bpy.ops.object.delete(use_global=False)

    try:
        # bpy.ops.wm.obj_import is the Blender 4+ OBJ importer.
        bpy.ops.wm.obj_import(filepath=src)
    except Exception as exc:
        results.append(f"ERROR {out_name}  import: {exc}")
        continue

    # Build a Principled BSDF material and assign it to every mesh.
    if textures:
        mat = bpy.data.materials.new(name=out_name.replace('.glb', '_mat'))
        mat.use_nodes = True
        nodes = mat.node_tree.nodes
        links = mat.node_tree.links
        nodes.clear()

        bsdf     = nodes.new('ShaderNodeBsdfPrincipled')
        out_node = nodes.new('ShaderNodeOutputMaterial')
        links.new(bsdf.outputs['BSDF'], out_node.inputs['Surface'])

        def _tex(path, colorspace='sRGB'):
            img = bpy.data.images.load(path)
            img.colorspace_settings.name = colorspace
            node = nodes.new('ShaderNodeTexImage')
            node.image = img
            return node

        # Albedo × AO → Base Color
        if 'albedo' in textures:
            alb = _tex(textures['albedo'])
            if 'ao' in textures:
                ao  = _tex(textures['ao'], 'Non-Color')
                mix = nodes.new('ShaderNodeMixRGB')
                mix.blend_type = 'MULTIPLY'
                mix.inputs['Fac'].default_value = 1.0
                links.new(alb.outputs['Color'], mix.inputs['Color1'])
                links.new(ao.outputs['Color'],  mix.inputs['Color2'])
                links.new(mix.outputs['Color'], bsdf.inputs['Base Color'])
            else:
                links.new(alb.outputs['Color'], bsdf.inputs['Base Color'])

        # Normal map
        if 'normal' in textures:
            nrm      = _tex(textures['normal'], 'Non-Color')
            nrm_node = nodes.new('ShaderNodeNormalMap')
            links.new(nrm.outputs['Color'],      nrm_node.inputs['Color'])
            links.new(nrm_node.outputs['Normal'], bsdf.inputs['Normal'])

        # Gloss → Roughness (inverted)
        if 'gloss' in textures:
            gls    = _tex(textures['gloss'], 'Non-Color')
            invert = nodes.new('ShaderNodeInvert')
            links.new(gls.outputs['Color'],    invert.inputs['Color'])
            links.new(invert.outputs['Color'], bsdf.inputs['Roughness'])

        # Specular intensity — socket was renamed in Blender 4.0
        if 'specular' in textures:
            spc = _tex(textures['specular'], 'Non-Color')
            spec_socket = (
                bsdf.inputs.get('Specular IOR Level') or
                bsdf.inputs.get('Specular')
            )
            if spec_socket:
                links.new(spc.outputs['Color'], spec_socket)

        for obj in bpy.data.objects:
            if obj.type == 'MESH':
                obj.data.materials.clear()
                obj.data.materials.append(mat)

    try:
        bpy.ops.export_scene.gltf(
            filepath=out,
            export_format='GLB',
            export_apply=True,
        )
        results.append(f"OK    {out_name}")
    except Exception as exc:
        results.append(f"ERROR {out_name}  export: {exc}")

# ── Report ─────────────────────────────────────────────────────────────────
print("\n─── Conversion results ───────────────────────────────")
for line in results:
    print(line)
print("─────────────────────────────────────────────────────")
print(f"Output: {OUT}")
