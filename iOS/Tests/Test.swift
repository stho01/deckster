import Testing

struct Test {

    @Test func something() async throws {
        #expect(1 == 2)
    }
}
