module.exports = function () {
	var webroot = "./Layouts/SPD2016/";
	var client = "./client/";
	var config = {
		libs: client + "libs/*.js",
		plugins: client + "plugins/*.js",
		destjs: webroot + "script/libs.min.js",
		tsFiles: [
			client + "app/**/*.ts"
		],
		templates: [
			client + "templates/*.html"
		],
		transpiledFiles: webroot + "script/",
		templatesDest: webroot + "templates/",
		destPath: webroot + "script"
	};
	return config;
}