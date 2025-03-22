input("*.prefab")
{
	string("filter", "");
	ulong("minFileId","65536");
	int("pathwidth","480");
    feature("source", "project");
    feature("menu", "1.Project Resources/Check Prefab File ID");
	feature("description", "just so so");
}
filter
{
    if(assetpath.Contains(filter)){
        $v0 = loadasset(assetpath);
        $v1 = getcomponentsinchildren($v0,"MonoBehaviour");
        looplist($v1){
            $v2 = $$;
            if(isnull($v2)){
                $v99 = newitem();
                $v99.AssetPath = assetpath;
                $v99.Info = "component is null.";
            }else{
                $v3 = getguidandfileid($v2);
                if($v3.Value<minFileId){
                    $v99 = newitem();
                    $v99.AssetPath = assetpath;
                    $v99.Info = format("name:{0} guid:{1} file_id:{2}", $v2.name, $v3.Key, $v3.Value);
                };
            };
        };
    };
    0;
};