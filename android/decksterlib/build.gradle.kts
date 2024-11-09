import Build_gradle.FixClassPackagesAfterOpenApi.Companion.PackageSeparator
import org.openapitools.generator.gradle.plugin.tasks.GenerateTask
import java.util.regex.Pattern
import kotlin.io.path.Path
import java.io.File as JFile

val openApiYmlFile = "$projectDir/../../decksterapi.yml"

plugins {
    id("java-library")
    alias(libs.plugins.jetbrains.kotlin.jvm)
    alias(libs.plugins.openapi.generator)
}

java {
    sourceCompatibility = JavaVersion.VERSION_17
    targetCompatibility = JavaVersion.VERSION_17
    sourceSets["main"].java {
        srcDir("src-gen/src/main/kotlin")
    }
}

kotlin {
    jvmToolchain(17)

}

tasks.test {
    systemProperty("gameId", System.getProperty("gameId"))
    systemProperty("userId", System.getProperty("userId"))
    systemProperty("password", System.getProperty("password"))
}

dependencies {
    implementation(libs.okhttp)
    implementation(libs.jackson.annotations)
    implementation(libs.jackson.kotlin)
    implementation(libs.retrofit)
    implementation(libs.retrofit.converter.scalars)
    implementation(libs.retrofit.converter.jackson)
    implementation(libs.kotlinx.coroutines)
    testImplementation(libs.junit)
    implementation(libs.okhttp)
    implementation(libs.okhttp.logging)
}

tasks.register("generateDtos", GenerateTask::class.java) {
    dependsOn("openApiPreProcess")
    val packageRoot = "no.forse.decksterlib"
    // IMPORANT: DELETE build.gradle GENERATED BY THIS TASK, OR STUFF WON'T COMPILE
    // open-api-generate docs: https://github.com/OpenAPITools/openapi-generator/blob/master/modules/openapi-generator-gradle-plugin/README.adoc
    group = "openapi"
    println ("$projectDir")
    description = "Generate DTO classes for DecksterLib"
    generatorName.set("kotlin")
    // kotlin generator docs: https://openapi-generator.tech/docs/generators/kotlin/
    // Templates: https://github.com/OpenAPITools/openapi-generator/blob/master/modules/openapi-generator/src/main/resources/kotlin-client/data_class.mustache
    verbose.set(false)
    cleanupOutput.set(true)
    templateDir.set("$projectDir/openapi-templates")
    outputDir.set("$projectDir/build/openapi-temp")
    skipValidateSpec.set(false)

    val sourceTask = project.getTasksByName("openApiPreProcess", false).first() as ReplacePackeSeparator
    inputSpec.set(sourceTask.outputFile.get().toString())

    ignoreFileOverride.set("$projectDir/.openapi-generator-ignore")
    packageName.set("${packageRoot}.rest")
    apiPackage.set("${packageRoot}.rest")
    modelPackage.set("${packageRoot}.model")
    library.set("jvm-retrofit2")
    configOptions = mapOf(
        "serializationLibrary" to "jackson",
        "useCoroutines" to "true"
    )
    generateModelDocumentation.set(false)
    generateApiDocumentation.set(false)
    generateApiTests.set(false)
    generateModelTests.set(false)
    openapiNormalizer.set(mapOf("REF_AS_PARENT_IN_ALLOF" to "true"))
}


abstract class ReplacePackeSeparator : DefaultTask() {
    @InputFile
    @PathSensitive(PathSensitivity.ABSOLUTE)
    val inputFile = project.objects.property<java.io.File>()

    @OutputFile
    val outputFile = project.objects.property<java.io.File>()

    @TaskAction
    fun replacePackageSeparator() {
        var startPointReached = false
        val outFile = outputFile.get()
        val outLines = mutableListOf<String>()
        inputFile.get().forEachLine { line ->
            val lineToWrite = if (startPointReached) {
                line.replace(".", PackageSeparator)
            } else line
            outLines.add(lineToWrite)
            if (line.startsWith("components:") || line.startsWith("paths:")) {
                startPointReached = true
            }
        }
        outFile.writeText(outLines.joinToString(System.lineSeparator()), Charsets.UTF_8)
    }
}

abstract class FixClassPackagesAfterOpenApi : DefaultTask() {
    companion object {
        val PackageRoot = "no.forse.decksterlib.model"
        val PackageSeparator = "XXX"
        val NL = System.lineSeparator()
    }

    @InputDirectory
    @PathSensitive(PathSensitivity.ABSOLUTE)
    val inputDir = project.objects.property<java.io.File>()

    @OutputDirectory
    val outputDir = project.objects.property<java.io.File>()

    @TaskAction
    fun splitToPackages() {
        val modelPath = inputDir.get()
        logger.info("Processing files from '$modelPath'...")
        val generatedFiles = modelPath.listFiles()!!
        logger.info("${generatedFiles.size} files found...")
        outputDir.get().deleteRecursively()
        outputDir.get().mkdirs()
        for (file in generatedFiles) {
            if (!file.name.contains(PackageSeparator)) continue
            val (packageNameUpr, fileName) = file.name.split(PackageSeparator)
            val packageName = packageNameUpr.lowercase()
            val fullPackage = "$PackageRoot.$packageName"
            val targetDir = Path(outputDir.get().toString(), packageName).toFile()
            targetDir.mkdirs()
            val destFile = Path(targetDir.toString(), fileName).toFile()
            copyFileAndAlterPackage(file, destFile, packageNameUpr, fullPackage)
            logger.info("Package: $fullPackage File: $fileName")
        }
    }

    fun copyFileAndAlterPackage(source: JFile, dest: JFile, packageNameUpr: String, fullPackage: String) {
        dest.delete()
        val regex = Pattern.compile("(\\w+)$PackageSeparator").toRegex()
        val jsonSubtyperegex = Pattern.compile("(\\w+)$PackageSeparator(\\w+)::class").toRegex()
        source.readLines(Charsets.UTF_8).map { line ->
            if (line.startsWith("package")) {
                "package $fullPackage"
            } else {
                line
                    .replace(jsonSubtyperegex) { res ->
                        "$PackageRoot." + res.groupValues[1].lowercase() + "." + res.groupValues[2] + "::class" // DecksterMessage.kt
                    }
                    .replace(regex) { res ->
                        if (line.contains("import")) {
                            res.groupValues[1].lowercase() + "." // In import
                        } else if (line.contains("JsonSubTypes")) {
                            res.groupValues[1] + "." // In import
                        } else {
                           "" // in class definition, extends
                        }
                    }
                    .replace("${packageNameUpr}$PackageSeparator", "")
            }
        }.forEach {
            dest.appendText(it + NL, Charsets.UTF_8)
        }
    }
}

tasks.register<ReplacePackeSeparator>("openApiPreProcess") {
    group = "openapi"
    description = "Replace dot-separator for package names with characters allowed in class names in Kotlin." +
            "Package name will temporarily be part of the class name until openApiPostProcess is run after openApi generation"

    val outFile = Path(project.projectDir.toString(), "build", "openapi-preprocess", "opeanpi_nodot.yml")
    outputFile.set(outFile.toFile())

    val inFile = JFile(openApiYmlFile)
    inputFile.set(inFile)
}

tasks.register<FixClassPackagesAfterOpenApi>("openApiPostProcess") {
    group = "openapi"
    description = "Since stupid OpenApi does not support package name. We have to fix it. I hate having to do stuff"
    dependsOn("generateDtos")
    val outputRoot = Path(
        project.projectDir.toString(), "src-gen", "src", "main", "kotlin" ,"no", "forse", "decksterlib", "model"
    )
    outputDir.set(outputRoot.toFile())
    val sourceTask = project.getTasksByName("generateDtos", false).first() as GenerateTask
    val sourceDirRoot = sourceTask.outputDir.get()
    val sourceModelPath = sourceTask.modelPackage.get().replace(".", JFile.separator)
    val sourceDir = Path(sourceDirRoot, "src", "main", "kotlin", sourceModelPath).toFile()
    inputDir.set(sourceDir)
}

project.getTasksByName("compileKotlin", false).first().dependsOn("openApiPostProcess")
