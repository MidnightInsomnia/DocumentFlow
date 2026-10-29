using Microsoft.AspNetCore.Identity;

namespace DocumentFlow.Api.Auth;

public static class IdentityApiErrors
{
    public static object From(IdentityResult result) => new
    {
        message = string.Join(" ", result.Errors.Select(error => error.Code switch
        {
            "PasswordTooShort" => "Пароль слишком короткий (минимум 12 символов).",
            "PasswordRequiresDigit" => "Пароль должен содержать цифру.",
            "PasswordRequiresLower" => "Пароль должен содержать строчную букву.",
            "PasswordRequiresUpper" => "Пароль должен содержать заглавную букву.",
            "PasswordRequiresNonAlphanumeric" => "Пароль должен содержать специальный символ.",
            "DuplicateUserName" => "Логин уже используется.",
            "DuplicateEmail" => "Email уже используется.",
            "InvalidUserName" => "Некорректный логин.",
            "InvalidEmail" => "Некорректный email.",
            "PasswordMismatch" => "Неверный текущий пароль.",
            _ => "Не удалось сохранить пользователя. Проверьте введённые данные."
        }).Distinct())
    };
}
