input("*.fbx","*.FBX")
{
	string("contains", "");
	string("not_contains", "");
	bool("optimize", true);
	feature("source", "project");
	feature("menu", "2.Project Resources/UnOptimize Game Object");
	feature("description", "just so so");
}
filter
{
	if(asset_path.Contains(contains) && (is_null_or_empty(not_contains) || !asset_path.Contains(not_contains))){
		if(optimize && !importer.optimizeGameObjects){
			0;
		}else{
			1;
		};
	}else{
		0;
	};
}
process
{
	importer.optimizeGameObjects = false;
  save_and_reimport();
};