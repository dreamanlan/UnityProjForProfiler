input("*.fbx","*.FBX")
{
	string("contains", "");
	string("not_contains", "");
	string("not_contains2", "");
	string("prop",""){
		multiple(["not optimize","empty extra exposed"],[1,2]);
	};
	feature("source", "project");
	feature("menu", "2.Project Resources/Optimize Game Object");
	feature("description", "just so so");
}
filter
{
	if(asset_path.Contains(contains) && (is_null_or_empty(not_contains) || !(asset_path.Contains(not_contains) && !asset_path.Contains(not_contains2)))){
		if(prop.Contains("1") && importer.optimizeGameObjects || prop.Contains("2") && !is_null(importer.extraExposedTransformPaths) && importer.extraExposedTransformPaths.Length>0){
			0;
		}else{
			object = load_asset(asset_path);
			1;
		};
	}else{
		0;
	};
}
process
{
	importer.optimizeGameObjects = true;
	set_extra_exposed_transform_paths(object, "*");
    save_and_reimport();
};