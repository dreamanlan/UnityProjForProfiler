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
	if(stringcontains(assetpath,filter)){
		$v0 = loadasset(assetpath);
		$v1 = $v0.name;
		$v2 = getruntimememory($v0);
		$v3 = singlemeshinfo($v0);
		unloadasset($v0);
		if(isnull($v3) || (readable && !$v3.isReadable)){
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