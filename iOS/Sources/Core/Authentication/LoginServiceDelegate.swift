import Foundation

public protocol LoginServiceDelegate: AnyObject {
    func didLoginSuccessfully(userModel: UserModel)
    func didFailToLogin(error: Error)
}
