module.exports = function(grunt) {

  grunt.initConfig({
    watch: {
      scripts: {
        files: ['**/*.cs'],
        tasks: ['shell:ps'],
        options: {spawn: false, cwd:'C:\\Git\\Repositories\\UnityClient\\src\\UnityClient\\'},
      },
    },
    shell: {
      ps: {
        options: {stdout: true},
        command: 'powershell C:\\Git\\Repositories\\Gists\\CS-merger.ps1 -source C:\\Git\\Repositories\\UnityClient\\src\\UnityClient\\ -output C:\\Git\\Repositories\\UnityClient\\unity\\Assets\\RestBehaviour.cs -copyAsIs ScoreboardService >> C:\\Git\\Repositories\\UnityClient\\automation\\cs-merger\\cs-merger.log'
      }
    }
  });

  grunt.loadNpmTasks('grunt-shell');
  grunt.loadNpmTasks('grunt-contrib-watch');
  grunt.registerTask('default', ['watch']);
}
