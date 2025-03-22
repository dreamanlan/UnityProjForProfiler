input("*.tga","*.png","*.jpg","*.exr")
{
	int("maxSize",1){
		range(1,1024);
	};
	string("prop",""){
		multiple(["readable","mipmap"],[1,2]);
	};
	stringlist("filter", "");
	stringlist("notfilter", "");
	stringlist("anyfilter", "t_XueNu|t_GaoYue_luoli|t_DuanMuRong|t_ShaoSiMing|t_DaSiMing|t_XingHun|t_XiangShaoYu|m3a0_ChuLingEr|t_ChiLian|t_GeNie|t_ShiLan|t_TianMing|t_YanDan_DouLi|t_GaoJianLi|t_ZhangLiang|t_WeiZhuang|t_GongSunLingLong|t_LongJu_JiaZhou|t_GaoYue_YinYangJia");
	stringlist("anynotfilter", "");
	float("pathwidth",240){range(20,4096);};
	feature("source", "project");
	feature("menu", "1.Project Resources/Revert Sized Textures");
	feature("description", "just so so");
}
filter
{
    if(stringcontains(assetpath, filter) && stringnotcontains(assetpath, notfilter) && stringcontainsany(assetpath, anyfilter) && stringnotcontainsany(assetpath, anynotfilter)){
    	$v0 = loadasset(assetpath);
    	if(isnull($v0)){
    		0;
    	} else {
    		$v1 = $v0.width;
    		$v2 = $v0.height;
    		$v3 = importer.isReadable;
    		$v4 = importer.mipmapEnabled;

    		$v5 = gettexturesetting("iPhone");
        	$v6 = gettexturesetting("Android");

    		//unloadasset($v0);
    		order = $v1 < $v2 ? $v2 : $v1;
    		if(($v1 > maxSize || $v2 > maxSize) && ($v5.maxTextureSize<$v1 && $v5.maxTextureSize<$v2 || $v6.maxTextureSize<$v1 && $v6.maxTextureSize<$v2) && (prop.Contains("1") && $v3 || !prop.Contains("1")) && (prop.Contains("2") && $v4 || !prop.Contains("2"))){
    			info = format("size:{0},{1} readable:{2} mipmap:{3}", $v1, $v2, $v3, $v4);
    			1;
    		} else {
    		    0;
    		};
    	};
    }else{
        0;
    };
}
process
{
	/*
	$v0 = getdefaulttexturesetting();
	$v0.maxTextureSize = changetype(maxSize, "int");
	settexturesetting($v0);

	$v1 = gettexturesetting("Standalone");
	$v1.maxTextureSize = changetype(maxSize, "int");
	settexturesetting($v1);
	*/
	$v2 = gettexturesetting("iPhone");
	$v2.overridden=true;
	$v2.maxTextureSize = 2048;
	setastctexture($v2);
	settexturesetting($v2);

	$v3 = gettexturesetting("Android");
	$v3.overridden=true;
	$v3.maxTextureSize = 2048;
	setastctexture($v3);
	settexturesetting($v3);

  saveandreimport();
};