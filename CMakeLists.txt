cmake_minimum_required(VERSION 3.22)
project(libESPER VERSION 1.0.0 LANGUAGES C)

include(CTest)
enable_testing()

add_library(esper SHARED
    src/fft.c)
#    esper.def)
target_include_directories(esper PRIVATE "${PROJECT_SOURCE_DIR}/")
target_compile_definitions(esper PRIVATE LIBESPER_BUILD)
set_property(TARGET esper PROPERTY C_STANDARD 17)
set_property(TARGET esper PROPERTY C_VISIBILITY_PRESET hidden)
set_property(TARGET esper PROPERTY OUTPUT_NAME "esper")
set_property(TARGET esper PROPERTY WINDOWS_EXPORT_ALL_SYMBOLS ON)

add_executable(test_esper
    src/test.c)
target_include_directories(test_esper PRIVATE "${PROJECT_SOURCE_DIR}/")
target_compile_definitions(test_esper PRIVATE LIBESPER_BUILD)
set_property(TARGET test_esper PROPERTY C_STANDARD 17)

target_link_libraries(test_esper PRIVATE esper)

# Add multiple tests with different arguments
set(TEST_ARGS_LIST
    "arg1"
    "arg2"
    "arg3"
    "arg4"
)

foreach(TEST_ARG IN LISTS TEST_ARGS_LIST)
    add_test(NAME test_esper_${TEST_ARG} COMMAND test_esper ${TEST_ARG} WORKING_DIRECTORY "${PROJECT_SOURCE_DIR}/")
endforeach()
