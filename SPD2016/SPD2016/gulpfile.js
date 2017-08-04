var gulp = require("gulp");
var config = require("./gulp.config")();
var $ = require("gulp-load-plugins")({lazy: true});

// JS stuff

gulp.task("default", ["min-js", "transpile"], function () {
    gulp.watch(config.tsFiles, ["transpile"]).on("change", reportChange);
	gulp.watch(config.js, ["min-js"]).on("change", reportChange);
});

gulp.task("min-js", function () {
    return gulp.src(["./node_modules/systemjs/dist/system.js", config.libs, config.plugins])
		.pipe($.plumber())
        .pipe($.concat(config.destjs))
		.pipe($.uglify())
        .pipe(gulp.dest("."))
		.pipe(gulp.dest("C:\\Program Files\\Common Files\\microsoft shared\\Web Server Extensions\\16\\TEMPLATE\\"));
});

gulp.task("transpile", function () {
    return gulp
		.src(config.tsFiles)
		.pipe($.plumber())
		.pipe($.typescript())
		.pipe($.uglify())
		.pipe(gulp.dest(config.transpiledFiles))
		.pipe(gulp.dest("C:\\Program Files\\Common Files\\microsoft shared\\Web Server Extensions\\16\\TEMPLATE\\Layouts\\SPD2016\\script\\"));
});

// Templates stuff
gulp.task("copy-templates", function(){
	return gulp
		.src(config.templates)
		.pipe(gulp.dest(config.templatesDest))
		.pipe(gulp.dest("C:\\Program Files\\Common Files\\microsoft shared\\Web Server Extensions\\16\\TEMPLATE\\Layouts\\SPD2016\\templates\\"));
});

// Clean stuff

gulp.task("clean", ["clean:js"]);

gulp.task("clean:js", function (cb) {
    del(config.concatJsDest, cb);
});

function reportChange(event) {
    console.log("File " + event.path + " was " + event.type + ", running tasks...");
}