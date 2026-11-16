input("*.asset")
{
    stringlist("filter","");
    bool("readable", false);
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Mesh Assets");
	feature("description", "just so so");
}
filter
{
	if(string_contains(asset_path,filter)){
		$v0 = load_asset(asset_path);
		$v1 = $v0.name;
		$v2 = get_runtime_memory_size($v0);
		$v3 = single_mesh_info($v0);
		unload_asset($v0);
		if(is_null($v3) || (readable && !$v3.isReadable)){
			0;
		}else{
			value = $v2;
			order = $v2;
			info = "name:" + $v1 + " mem:" + $v2 + " readable:" + $v3.isReadable + " submesh:" + $v3.subMeshCount
				+ " bindpose:" + $v3.bindposeCount + " blendshape:" + $v3.blendShapeCount
				+ " vertexes:" + $v3.vertexCount + " triangles:" + $v3.triangleCount;
			1;
		};
    }else{
        0;
    };
};