input("*.fbx")
{
	string("contains", "");
	string("not_contains", "");
	bool("optimize", true);
	feature("source", "project");
	feature("menu", "1.Project Resources/UnOptimize Game Object");
	feature("description", "just so so");	
}
filter
{
	if(assetpath.Contains(contains) && (isnullorempty(not_contains) || !assetpath.Contains(not_contains))){
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
  saveandreimport();
};