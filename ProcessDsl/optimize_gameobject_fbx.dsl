input("*.fbx")
{
	string("contains", "");
	string("not_contains", "Player");	
	string("not_contains2", "PoTuSanLang");	
	string("prop",""){
		multiple(["not optimize","empty extra exposed"],[1,2]);
	};
	feature("source", "project");
	feature("menu", "1.Project Resources/Optimize Game Object");
	feature("description", "just so so");	
}
filter
{
	if(assetpath.Contains(contains) && (isnullorempty(not_contains) || !(assetpath.Contains(not_contains) && !assetpath.Contains(not_contains2)))){
		if(prop.Contains("1") && importer.optimizeGameObjects || prop.Contains("2") && !isnull(importer.extraExposedTransformPaths) && importer.extraExposedTransformPaths.Length>0){
			0;
		}else{
			object = loadasset(assetpath);
			1;
		};
	}else{
		0;
	};
}
process
{
	importer.optimizeGameObjects = true;
	setextraexposedtransformpaths(object, "*");
    saveandreimport();
};